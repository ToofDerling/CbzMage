using CbzMage.Shared.Extensions;
using MobiMetadata;

namespace AzwConverter.Engine
{
    public class SaveFileCoverEngine : SaveBookCoverEngine
    {
        private List<Azw6Head>? _hdHeaderList;

        private FileInfo? _azwFile;

        public async Task<CbzItem> SaveFileCoverAsync(FileInfo azwFile, List<Azw6Head> hdHeaderList)
        {
            _hdHeaderList = hdHeaderList;

            _azwFile = azwFile;

            return await ReadImageDataAsync(azwFile.Name, azwFile);
        }

        public string GetCoverFile()
        {
            return _coverFile;
        }

        protected override FileInfo? SelectHDContainer(FileInfo[] dataFiles)
        {
            IgnoreHDContainerWarning = true;

            _coverFile = GetCoverFile(_azwFile!.FullName, Metadata!.MobiHeader.GetFullTitle());

            return HDContainerHelper.FindHDContainer(Metadata!, _hdHeaderList);
        }

        private static string GetCoverFile(string azwFile, string title)
        {
            title = $"{title.ToFileSystemString()}.jpg";

            if (!string.IsNullOrEmpty(Settings.SaveCoverDir))
            {
                return Path.Combine(Settings.SaveCoverDir, title);
            }

            var dir = Path.GetDirectoryName(azwFile);
            return Path.Combine(dir!, title);
        }
    }
}