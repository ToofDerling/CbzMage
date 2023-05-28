namespace AzwConverter.Engine
{
    public class ScanBookEngine : AbstractImageEngine
    {
        public async Task<CbzItem> ScanBookAsync(string bookId, FileInfo[] dataFiles) => await ReadImageDataAsync(bookId, dataFiles);

        protected override Task<CbzItem> ProcessImagesAsync() => Task.FromResult(ReadCbzState());

        private CbzItem ReadCbzState()
        {
            var state = new CbzItem
            {
                HdCover = Metadata.IsHdCover(),
                SdCover = Metadata.IsSdCover(),
            };

            for (int i = 0, sz = Metadata.MergedImageRecords.Count; i < sz; i++)
            {
                state.Pages++;

                if (Metadata.IsHdPage(i))
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