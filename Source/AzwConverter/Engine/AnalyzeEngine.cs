using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using ImageMagick;
using MobiMetadata;

namespace AzwConverter.Engine
{
    public class AnalyzeEngine : AbstractImageEngine
    {
        private string _bookDir;
        private string _analyzeMessage;

        public async Task<CbzState> AnalyzeBookAsync(string bookId, FileInfo[] dataFiles, string bookDir)
        {
            _bookDir = bookDir;

            return await ReadMetaDataAsync(bookId, dataFiles);
        }

        public string GetAnalyzeMessage()
        { 
            return _analyzeMessage; 
        }

        protected override async Task<CbzState?> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords)
            => await AnalyzeBookAsync(pageRecordsHd, pageRecords);

        private async Task<CbzState> AnalyzeBookAsync(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var state = new CbzState();

            var sdDir = Path.Combine(_bookDir, "SD");

            string hdDir = null;
            if (hdImageRecords != null)
            {
                hdDir = Path.Combine(_bookDir, "HD");
            }

            const string coverName = "cover.jpg";
            var name = coverName;

            // HD cover
            if (hdImageRecords != null && hdImageRecords.CoverRecord != null)
            {
                state.HdCover = true;
            }

            // SD cover
            if (sdImageRecords.CoverRecord != null)
            {
                state.SdCover = true;
            }

            if (!await IsUnexpectedHdRecordAsync(hdImageRecords?.CoverRecord, hdDir, name))
            {
                //SaveRecords(sdImageRecords, sdDir);
                //SaveRecords(hdImageRecords, hdDir);
                await CompareRecordsAsync(name, hdImageRecords?.CoverRecord, hdDir, sdImageRecords.CoverRecord, sdDir);
            }

            // pages
            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                // SD
                state.SdImages++;
                state.Pages++;

                // HD
                if (hdImageRecords != null)
                {
                    var hdRecord = hdImageRecords.ContentRecords[i];
                    if (await hdRecord.IsCresRecordAsync())
                    {
                        state.HdImages++;
                    }

                    name = i.ToString().PadLeft(4, '0');

                    if (!await IsUnexpectedHdRecordAsync(hdRecord, hdDir, name))
                    {
                        //SaveRecords(sdImageRecords, sdDir);
                        //SaveRecords(hdImageRecords, hdDir);
                        await CompareRecordsAsync(name, hdRecord, hdDir, sdImageRecords.ContentRecords[i], sdDir);
                    }
                }
            }

            return state;
        }

        public async Task<bool> IsUnexpectedHdRecordAsync(PageRecord? hdRecord, string hdDir, string name)
        {
            // Length of those hd container records not meant to replace sd records is 4 (presumably because of the CRES marker)
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

            for (var i = 1; i <= records.ContentRecords.Count - 1; i++)
            {
                var rec = records.ContentRecords[i];

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
            sdImage.Ping(sdStream);

            var hdStream = new MemoryStream();
            await hdRecord.WriteDataAsync(hdStream);
            using var hdImage = new MagickImage();
            hdImage.Ping(hdStream);

            // If SD cover is "better than" HD cover, save both and have a look.
            if (sdImage.Height > hdImage.Height || sdImage.Quality > hdImage.Quality)
            {
                _analyzeMessage = $"Height SD: {sdImage.Height} vs HD: {hdImage.Height}. Quality SD {sdImage.Quality} vs HD {hdImage.Quality}";

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