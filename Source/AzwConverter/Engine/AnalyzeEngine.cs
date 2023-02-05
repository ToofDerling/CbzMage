using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using ImageMagick;
using MobiMetadata;
using System.Text;

namespace AzwConverter.Engine
{
    public class AnalyzeEngine : AbstractImageEngine
    {
        private string _bookDir;
        private string _analyzeMessageOk;
        private string _analyzeMessageError;

        private bool _analyzeImages;

        private string _bookId;

        private bool _analysisDir;

        public async Task<CbzState> AnalyzeBookAsync(string bookId, FileInfo[] dataFiles, bool analyzeImages, string bookDir)
        {
            _analyzeImages = analyzeImages;
            _bookDir = bookDir;

            _bookId = bookId;

            _analysisDir = !string.IsNullOrEmpty(Settings.AnalysisDir);

            IgnoreHDContainerWarning = true;

            return await ReadImageDataAsync(bookId, dataFiles);
        }

        public string GetAnalyzeMessageOk()
        {
            return _analyzeMessageOk;
        }

        public string GetAnalyzeMessageError()
        {
            return _analyzeMessageError;
        }

        protected override async Task<CbzState> ProcessImagesAsync()
            => await AnalyzeBookAsync();

        private async Task<CbzState> AnalyzeBookAsync()
        {
            var state = new CbzState();

            //var messageOk = new StringBuilder();
            var messageError = new StringBuilder();

            var bookType = Metadata.MobiHeader.ExthHeader.BookType;

            if (!bookType.EqualsIgnoreCase("comic"))
            {
                AppendMsg(messageError, bookType);
            }

            //foreach (var record in sdImageRecords.ImageRecords) 
            //{
            //    if (await record.IsFontRecordAsync())
            //    {
            //        AppendMsg(messageError, "FONT");
            //    }
            //}

            //if (hdImageRecords != null)
            //{
            //    if (Metadata.Azw6Header.Title != Metadata.MobiHeader.ExthHeader.UpdatedTitle)
            //    {
            //        var msg = $"[{Metadata.Azw6Header.Title}] vs [{Metadata.MobiHeader.ExthHeader.UpdatedTitle}]";
            //        AppendMsg(messageError, msg);
            //    }
            //}

            //if (hdImageRecords != null)
            //{
            //    var rescRecords = sdImageRecords.ContentRecords.Count;
            //    if (sdImageRecords.CoverRecord != null)
            //    {
            //        rescRecords++;
            //    }

            //    if (Metadata.Azw6Header.RescRecordsCount != rescRecords)
            //    {
            //        var msg = $"hd: {Metadata.Azw6Header.RescRecordsCount} sd: {rescRecords}";
            //        AppendMsg(messageError, msg);
            //    }
            //}

            if (Metadata.HdContainerRecords != null)
            {
                var cresCount = 0;
                var placeHolderCount = 0;

                foreach (var hdImageRecord in Metadata.HdContainerRecords.ImageRecords)
                {
                    var isCres = await hdImageRecord.IsCresRecordAsync();
                    if (isCres)
                    {
                        cresCount++;
                    }

                    var isPlaceHolder = hdImageRecord.IsCresPlaceHolder();
                    if (isPlaceHolder)
                    {
                        placeHolderCount++;
                    }

                    if (!isCres && !isPlaceHolder)
                    {
                        throw new Exception($"{Metadata.MobiHeader.ExthHeader.UpdatedTitle} isCres & isPlaceholder both false");
                    }
                }

                var total = cresCount + placeHolderCount;
                _analyzeMessageOk = $"{total} cres: {cresCount} placeholder: {placeHolderCount}";

                if (total != Metadata.PageRecords.RescRecord.PageCount)
                {
                    throw new Exception($"{Metadata.MobiHeader.ExthHeader.UpdatedTitle} {total} vs {Metadata.PageRecords.RescRecord.PageCount}");
                }
            }

            if (!_analyzeImages)
            {
                if (messageError.Length> 0) 
                {
                    _analyzeMessageError = messageError.ToString();
                }

                state.Pages = Metadata.PageRecords.ImageRecords.Count;
                return state;
            }

            var sdDir = Path.Combine(_bookDir, "SD");

            string hdDir = null;
            if (Metadata.HdContainerRecords != null)
            {
                hdDir = Path.Combine(_bookDir, "HD");
            }

            const string coverName = "cover.jpg";
            var name = coverName;

            // HD cover
            if (Metadata.HdContainerRecords != null && Metadata.HdContainerRecords.CoverRecord != null)
            {
                state.HdCover = true;
            }

            // SD cover
            if (Metadata.PageRecords.CoverRecord != null)
            {
                state.SdCover = true;
            }

            if (!await IsUnexpectedHdRecordAsync(Metadata.HdContainerRecords?.CoverRecord, hdDir, name))
            {
                await SaveRecordsAsync(Metadata.PageRecords, sdDir);
                await SaveRecordsAsync(Metadata.HdContainerRecords!, hdDir);
                //await CompareRecordsAsync(name, hdImageRecords?.CoverRecord, hdDir, sdImageRecords.CoverRecord, sdDir);
            }

            return state;
        }

        private static void AppendMsg(StringBuilder sb, string msg)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            };
            sb.Append(msg);
        }

        public async Task<bool> IsUnexpectedHdRecordAsync(PageRecord? hdRecord, string hdDir, string name)
        {
            // Length of those hd container records not meant to replace sd records is 4 (presumably because of the CRES identifier)
            // A non cres record with a length > 4 is an anomaly. So if we find one in the wild, save it and take a look.
            if (hdRecord != null && hdRecord.Length != 4 && !await hdRecord.IsCresRecordAsync())
            {
                hdDir.CreateDirIfNotExists();
                var path = Path.Combine(hdDir, $"{name}.jpg");

                ProgressReporter.Warning($"Unexpected hd container non-cres file: {path} len: {hdRecord.Length}");

                await SaveRecordDataAsync(hdRecord, path);

                return true;
            }
            return false;
        }

        private async Task SaveRecordsAsync(PageRecords records, string dir)
        {
            if (records == null)
            {
                return;
            }

            dir.CreateDirIfNotExists();

            if (records.RescRecord != null)
            {
                var xml = await records.RescRecord.GetPrettyPrintXmlAsync();

                var path = Path.Combine(dir, "resc.xml");
                await File.WriteAllTextAsync(path, xml, CancellationToken.None);
            }

            if (records.CoverRecord != null)
            {
                var name = await records.CoverRecord.IsCresRecordAsync() ? "cover-cres.jpg" : "cover.jpg";

                var path = Path.Combine(dir, name);
                await SaveRecordDataAsync(records.CoverRecord, path);
            }

            for (var i = 0; i < records.ImageRecords.Count; i++)
            {
                var rec = records.ImageRecords[i];

                var name = i.ToString().PadLeft(4, '0');
                if (await rec.IsCresRecordAsync())
                {
                    name += "-cres";
                }
                name += ".jpg";
                var path = Path.Combine(dir, name);

                await SaveRecordDataAsync(rec, path);
            }
        }

        private async Task SaveRecordDataAsync(PageRecord record, string file)
        {
            using var fileStream = File.Open(file, FileMode.Create, FileAccess.Write);
            await record.WriteDataAsync(fileStream);
        }

        public async Task CompareRecordsAsync(string name, PageRecord? hdRecord, string hdDir, PageRecord? sdRecord, string sdDir)
        {
            if (sdRecord == null || hdRecord == null || !await hdRecord.IsCresRecordAsync())
            {
                return;
            }

            var sdStream = new MemoryStream();
            await sdRecord.WriteDataAsync(sdStream);
            using var sdImage = new MagickImage();
            sdStream.Position = 0;
            sdImage.Ping(sdStream);

            var hdStream = new MemoryStream();
            await hdRecord.WriteDataAsync(hdStream);
            using var hdImage = new MagickImage();
            hdStream.Position = 0;
            hdImage.Ping(hdStream);

            // If SD cover is "better than" HD cover, save both and have a look.
            if (sdImage.Height > hdImage.Height || sdImage.Quality > hdImage.Quality)
            {
                _analyzeMessageOk = $"Height SD: {sdImage.Height} vs HD: {hdImage.Height}. Quality SD {sdImage.Quality} vs HD {hdImage.Quality}";

                sdDir.CreateDirIfNotExists();
                var sdPath = Path.Combine(sdDir, name);

                await SaveAsync(sdStream, sdPath);

                hdDir.CreateDirIfNotExists();
                var hdPath = Path.Combine(hdDir, name);

                await SaveAsync(hdStream, hdPath);
            }

            async Task SaveAsync(Stream stream, string file)
            {
                stream.Seek(0, SeekOrigin.Begin);

                using var fileStream = File.Open(file, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream);
            }
        }
    }
}