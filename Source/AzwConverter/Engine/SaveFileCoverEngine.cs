using MobiMetadata;

namespace AzwConverter.Engine
{
    public class SaveFileCoverEngine : SaveBookCoverEngine
    {
        private List<Azw6Head>? _hdHeaderList;

        public async Task<CbzState> SaveCoverFileAsync(FileInfo azwFile, List<Azw6Head> hdHeaderList)
        {
            _hdHeaderList = hdHeaderList;

            return await ReadImageDataAsync(azwFile.Name, azwFile);
        }

        protected override FileInfo? SelectHDContainer(FileInfo[] dataFiles)
        {
            IgnoreHDContainerWarning = true;

            return HDContainerHelper.FindHDContainer(Metadata!, _hdHeaderList);
        }
    }
}