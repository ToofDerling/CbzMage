using MobiMetadata;

namespace AzwConverter.Engine
{
    public class ScanFileEngine : ScanBookEngine
    {
        private List<Azw6Head>? _hdHeaderList;

        public async Task<CbzState> ScanFileAsync(FileInfo azwFile, List<Azw6Head> hdHeaderList)
        {
            _hdHeaderList = hdHeaderList;

            var state = await ReadImageDataAsync(azwFile.Name, azwFile);
            state.Name = Metadata!.MobiHeader.GetFullTitle();

            return state;
        }

        protected override FileInfo? SelectHDContainer(FileInfo[] dataFiles)
        {
            IgnoreHDContainerWarning = true;

            return HDContainerHelper.FindHDContainer(Metadata!, _hdHeaderList);
        }
    }
}