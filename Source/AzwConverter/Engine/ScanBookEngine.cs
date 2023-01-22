using MobiMetadata;

namespace AzwConverter.Engine
{
    public class ScanBookEngine : AbstractImageEngine
    {
        public async Task<CbzState> ScanBookAsync(string bookId, FileInfo[] dataFiles) 
            => await ReadImageDataAsync(bookId, dataFiles);

        protected override async Task<CbzState> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords) 
            => await ReadCbzStateAsync(pageRecordsHd, pageRecords);

        private static async Task<CbzState> ReadCbzStateAsync(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var state = new CbzState
            {
                HdCover = hdImageRecords != null && hdImageRecords.CoverRecord != null 
                    && await hdImageRecords.CoverRecord.IsCresRecordAsync()
            };

            if (!state.HdCover)
            {
                state.SdCover = sdImageRecords.CoverRecord != null;
            }

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;

                if (hdImageRecords != null 
                    && await hdImageRecords.ContentRecords[i].IsCresRecordAsync())
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