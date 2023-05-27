using CbzMage.Shared.Extensions;
using MobiMetadata;

namespace AzwConverter.Engine
{
    public class ConvertFileEngine : ConvertBookEngine
    {
        private List<Azw6Head>? _hdHeaderList;

        private FileInfo? _azwFile;

        public async Task<CbzItem> ConvertFileAsync(FileInfo azwFile, List<Azw6Head> hdHeaderList)
        {
            _azwFile = azwFile;
            _mappedArchiveLen = azwFile.Length;

            _hdHeaderList = hdHeaderList;

            var state = await ReadImageDataAsync(azwFile.Name, azwFile);
            state.Name = _cbzFile!;
        
            return state;
        }

        protected override FileInfo? SelectHDContainer(FileInfo[] dataFiles)
        {
            IgnoreHDContainerWarning = true;

            var hdContainer = HDContainerHelper.FindHDContainer(Metadata!, _hdHeaderList);
            
            if (hdContainer != null) 
            {
                _mappedArchiveLen += hdContainer.Length;
            }

            return hdContainer;
        }

        protected override async Task<CbzItem> ProcessImagesAsync()
        {
            _cbzFile = GetCbzFile(_azwFile!.FullName, Metadata!.MobiHeader.GetFullTitle());
            _coverFile = GetCoverFile(_cbzFile);

            return await CreateCbzAsync();
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
            if (!Settings.SaveCover)
            { 
                return null;
            }

            var cover = Path.ChangeExtension(cbzFile, ".jpg");

            if (!string.IsNullOrEmpty(Settings.SaveCoverDir))
            {
                return Path.Combine(Settings.SaveCoverDir, Path.GetFileName(cover));
            }

            return cover;
        }
    }
}