using MobiMetadata;

namespace AzwConverter.Engine
{
    public class MetadataEngine : AbstractImageEngine
    {
        public async Task<(MobiMetadata.MobiMetadata metadata, IDisposable[] disposables)> ReadMetadataAsync(FileInfo[] dataFiles)
            => await base.ReadMetadataAsync(dataFiles);

        protected override async Task<CbzState?> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords)
            => null;
    }
}