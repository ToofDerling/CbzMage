using CbzMage.Shared.IO;

namespace AzwConverter.Engine
{
    public class SaveBookCoverEngine : AbstractImageEngine
    {
        protected string? _coverFile;
        private string? _coverString;

        public async Task<CbzState> SaveBookCoverAsync(string bookId, FileInfo[] dataFiles, string coverFile)
        {
            _coverFile = coverFile;

            return await ReadImageDataAsync(bookId, dataFiles);
        }

        public string? GetCoverString()
        {
            return _coverString;
        }

        protected override async Task<CbzState> ProcessImagesAsync()
        {
            await SaveCoverAsync();
            return new CbzState();
        }

        private async Task SaveCoverAsync()
        {
            using var stream = AsyncStreams.AsyncFileWriteStream(_coverFile!);

            if (Metadata.MergedCoverRecord != null)
            {
                await Metadata.MergedCoverRecord.WriteDataAsync(stream);

                _coverString = Metadata.IsHdCover() ? "HD cover" : "SD cover";
            }
            else
            {
                await Metadata.MergedImageRecords[0].WriteDataAsync(stream);

                _coverString = Metadata.IsHdPage(0) ? "HD page 1" : "SD page 1";
            }
        }
    }
}