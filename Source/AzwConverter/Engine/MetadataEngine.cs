namespace AzwConverter.Engine
{
    public class MetadataEngine : AbstractImageEngine
    {
        public async Task<(MobiMetadata.MobiMetadata metadata, IDisposable[] disposables)> GetMetadataAsync(FileInfo[] dataFiles) => await ReadMetadataAsync(dataFiles);

        protected override Task<CbzItem> ProcessImagesAsync() => Task.FromResult(new CbzItem());
    }
}