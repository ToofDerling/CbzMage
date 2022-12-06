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

        public async Task<(CbzState state, string analyzeMessage)> AnalyzeBookAsync(string bookId, FileInfo[] dataFiles, string bookDir)
        {
            _bookDir = bookDir;

            var state = await ReadMetaDataAsync(bookId, dataFiles);

            return (state, _analyzeMessage);
        }

        protected override CbzState ProcessImages(PageRecords? pageRecordsHd, PageRecords pageRecords)
        {
            return AnalyzeBook(pageRecordsHd, pageRecords);
        }

        private CbzState AnalyzeBook(PageRecords? hdImageRecords, PageRecords sdImageRecords)
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

            if (!IsUnexpectedHdRecord(hdImageRecords?.CoverRecord, hdDir, name))
            {
                //SaveRecords(sdImageRecords, sdDir);
                //SaveRecords(hdImageRecords, hdDir);
                CompareRecords(name, hdImageRecords?.CoverRecord, hdDir, sdImageRecords.CoverRecord, sdDir);
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
                    if (hdRecord.IsCresRecord)
                    {
                        state.HdImages++;
                    }

                    name = i.ToString().PadLeft(4, '0');

                    if (!IsUnexpectedHdRecord(hdRecord, hdDir, name))
                    {
                        //SaveRecords(sdImageRecords, sdDir);
                        //SaveRecords(hdImageRecords, hdDir);
                        CompareRecords(name, hdRecord, hdDir, sdImageRecords.ContentRecords[i], sdDir);
                    }
                }
            }

            return state;
        }

        public bool IsUnexpectedHdRecord(PageRecord? hdRecord, string hdDir, string name)
        {
            // Length of those hd container records not meant to replace sd records is 4 (presumably because of the CRES marker)
            // A non cres record with a length > 4 is an anomaly. So if we find one in the wild, save it and take a look.
            if (hdRecord != null && !hdRecord.IsCresRecord && hdRecord.Length != 4)
            {
                hdDir.CreateDirIfNotExists();
                var path = Path.Combine(hdDir, $"{name}.jpg");

                ProgressReporter.Warning($"Unexpected hd container non-cres file: {path} len: {hdRecord.Length}");

                using var unexpectedStream = File.Open(path, FileMode.Create, FileAccess.Write);
                unexpectedStream.Write(hdRecord.ReadData());

                return true;
            }

            return false;
        }

        private void SaveRecords(PageRecords records, string dir)
        {
            if (records == null)
            {
                return;
            }

            dir.CreateDirIfNotExists();

            if (records.RescRecord != null)
            {
                var xml = records.RescRecord.GetPrettyPrintXml();
                var path = Path.Combine(dir, "resc.xml");
                File.WriteAllText(path, xml);
            }

            if (records.CoverRecord != null)
            {
                var name = records.CoverRecord.IsCresRecord ? "cover-cres.jpg" : "cover.jpg";
                var path = Path.Combine(dir, name);

                using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
                stream.Write(records.CoverRecord.ReadData());
            }

            for (var i = 1; i <= records.ContentRecords.Count - 1; i++)
            {
                var rec = records.ContentRecords[i];

                var name = i.ToString().PadLeft(4, '0');
                if (rec.IsCresRecord)
                {
                    name += "-cres";
                }
                name += ".jpg";
                var path = Path.Combine(dir, name);

                using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
                stream.Write(rec.ReadData());
            }
        }

        public void CompareRecords(string name, PageRecord? hdRecord, string hdDir, PageRecord? sdRecord, string sdDir)
        {
            if (sdRecord == null || hdRecord == null || !hdRecord.IsCresRecord)
            {
                return;
            }

            var sdData = sdRecord.ReadData();
            using var sdImage = new MagickImage();
            sdImage.Ping(sdData);

            var hdData = hdRecord.ReadData();
            using var hdImage = new MagickImage();
            hdImage.Ping(hdData);

            // If SD cover is "better than" HD cover, save both and have a look.
            if (sdImage.Height > hdImage.Height || sdImage.Quality > hdImage.Quality)
            {
                _analyzeMessage = $"Height SD: {sdImage.Height} vs HD: {hdImage.Height}. Quality SD {sdImage.Quality} vs HD {hdImage.Quality}";

                sdDir.CreateDirIfNotExists();
                var sdPath = Path.Combine(sdDir, name);

                using var sdStream = File.Open(sdPath, FileMode.Create, FileAccess.Write);
                sdStream.Write(sdData);

                hdDir.CreateDirIfNotExists();
                var hdPath = Path.Combine(hdDir, name);

                using var hdStream = File.Open(hdPath, FileMode.Create, FileAccess.Write);
                hdStream.Write(hdData);
            }
        }
    }
}