namespace AzwConverter.Engine
{
    public class ScanBookEngine : AbstractImageEngine
    {
        public async Task<CbzState> ScanBookAsync(string bookId, FileInfo[] dataFiles)
            => await ReadImageDataAsync(bookId, dataFiles);

        protected override Task<CbzState> ProcessImagesAsync() => Task.FromResult(ReadCbzState());

        private CbzState ReadCbzState()
        {
            var state = new CbzState
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