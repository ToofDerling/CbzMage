using AzwMetadata;

namespace AzwConverter.Engine
{
    public class ScanEngine : AbstractImageEngine
    {
        public CbzState ScanBook(string bookId, FileInfo[] dataFiles)
        {
            return ReadMetaData(bookId, dataFiles);
        }

        protected override CbzState ProcessImages(PageRecords? pageRecordsHd, PageRecords pageRecords)
        {
            return ReadCbzState(pageRecordsHd, pageRecords);
        }

        private CbzState ReadCbzState(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var state = new CbzState
            {
                HdCover = hdImageRecords != null && hdImageRecords.CoverRecord != null && hdImageRecords.CoverRecord.IsCresRecord
            };

            if (!state.HdCover)
            {
                state.SdCover = sdImageRecords.CoverRecord != null;
            }

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;

                if (hdImageRecords != null && hdImageRecords.ContentRecords[i].IsCresRecord)
                {
                    state.HdImages++;
                }
                else
                {
                    state.SdImages++;
                }
            }

            return state;
        }
    }
}