using AzwMetadata;

namespace AzwConverter.Engine
{
    public class CoverEngine : AbstractImageEngine
    {
        private string _coverFile;
        private string _coverString;

        public CbzState? SaveCover(string bookId, FileInfo[] dataFiles, string coverFile)
        {
            _coverFile = coverFile;

            return ReadMetaData(bookId, dataFiles);
        }

        public string GetCoverString()
        { 
            return _coverString;
        }

        protected override CbzState ProcessImages(PageRecords? pageRecordsHd, PageRecords pageRecords)
        {
            SaveCover(pageRecordsHd, pageRecords);
            return null;
        }

        private void SaveCover(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            PageRecord coverRecord;

            // First try HD cover
            if (hdImageRecords != null && hdImageRecords.CoverRecord != null && hdImageRecords.CoverRecord.IsCresRecord)
            {
                coverRecord = hdImageRecords.CoverRecord;
                _coverString = "HD cover";
            }
            // Then the SD cover
            else if (sdImageRecords.CoverRecord != null)
            {
                coverRecord = sdImageRecords.CoverRecord;
                _coverString = "SD cover";
            }
            // Then the first HD page
            else if (hdImageRecords != null && hdImageRecords.ContentRecords.Count > 0 && hdImageRecords.ContentRecords[0].IsCresRecord)
            {
                coverRecord = hdImageRecords.ContentRecords[0];
                _coverString = "HD page 1";
            }
            // Then the first SD page
            else
            {
                coverRecord = sdImageRecords.ContentRecords[9];
                _coverString = "SD page 1";
            }

            SaveFile(coverRecord.ReadData(), _coverFile);
        }
    }
}