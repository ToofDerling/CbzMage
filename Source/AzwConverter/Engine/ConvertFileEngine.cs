using MobiMetadata;

namespace AzwConverter.Engine
{
    public class ConvertFileEngine : ConvertBookEngine
    {
        private List<Azw6Head>? _hdHeaderList;

        private FileInfo? _azwFile;

        public async Task<CbzState> ConvertFileAsync(FileInfo azwFile, List<Azw6Head> hdHeaderList)
        {
            _azwFile = azwFile;
            _mappedArchiveLen = azwFile.Length;

            _hdHeaderList = hdHeaderList;

            var state = await ReadImageDataAsync(azwFile.Name, azwFile);
            state.Name = _cbzFile!;
        
            return state;
        }

        protected override MobiMetadata.MobiMetadata? GetCachedMobiMetadata(string bookId)
        {
            return null;
        }

        protected override FileInfo? SelectHDContainer(FileInfo[] dataFiles)
        {
            var hdContainer = HDContainerHelper.FindHDContainer(Metadata!, _hdHeaderList);
            
            if (hdContainer != null) 
            {
                _mappedArchiveLen += hdContainer.Length;
            }

            return hdContainer;
        }

        protected override void DisplayHDContainerWarning(string fileName, string title)
        {
            //NOP 
        }

        protected override async Task<CbzState> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords)
        {
            _cbzFile = GetCbzFile(_azwFile!.FullName, Metadata!.MobiHeader.GetFullTitle());
            _coverFile = GetCoverFile(_cbzFile);

            return await CreateCbzAsync(pageRecordsHd, pageRecords);
        }

        private static string GetCbzFile(string azwFile, string title)
        {
            title = $"{title.ToFileSystemString()}.cbz";

            if (!string.IsNullOrEmpty(Settings.CbzDir) && !Settings.CbzDirSetBySystem)
            {
                return Path.Combine(Settings.CbzDir, title);
            }

            var dir = Path.GetDirectoryName(azwFile);
            return Path.Combine(dir!, title);
        }

        private static string? GetCoverFile(string cbzFile)
        {
            return Settings.SaveCover ? Path.ChangeExtension(cbzFile, ".jpg") : null;
        }
    }
}