using AzwConverter.Engine;
using CbzMage.Shared;
using CbzMage.Shared.Helpers;
using MobiMetadata;

namespace AzwConverter.Converter
{
    public class AzwFileOrDirectoryConverter : BaseAzwConverter
    {
        private readonly string _fileOrDirectory;

        public AzwFileOrDirectoryConverter(CbzMageAction action, string fileOrDirectory)
            : base(action)
        {
            _fileOrDirectory = fileOrDirectory;

            if (!string.IsNullOrEmpty(Settings.CbzDir) && !Settings.CbzDirSetBySystem)
            {
                ProgressReporter.Info($"Cbz backups: {Settings.CbzDir}");
            }
            ProgressReporter.Info($"Cbz compression: {Settings.CompressionLevel}");
        }

        public async Task ConvertOrScanAsync()
        {
            if (string.IsNullOrEmpty(_fileOrDirectory))
            {
                throw new ArgumentNullException(nameof(_fileOrDirectory));
            }

            var azwFiles = new List<FileInfo>();
            var allFiles = new List<FileInfo>();

            if (File.Exists(_fileOrDirectory))
            {
                var fileInfo = new FileInfo(_fileOrDirectory);

                if (!fileInfo.IsAzwOrAzw3File())
                {
                    ProgressReporter.Error($"Not an azw or azw3 file: {_fileOrDirectory}");
                    return;
                }

                azwFiles.Add(fileInfo);

                allFiles.AddRange(fileInfo.Directory.GetFiles());
            }
            else if (Directory.Exists(_fileOrDirectory))
            {
                var directoryInfo = new DirectoryInfo(_fileOrDirectory);
                allFiles.AddRange(directoryInfo.GetFiles());

                azwFiles = allFiles.Where(file => file.IsAzwOrAzw3File()).ToList();
                if (azwFiles.Count == 0)
                {
                    ProgressReporter.Error($"No azw or azw3 files found in: {_fileOrDirectory}");
                    return;
                }
            }
            else
            {
                ProgressReporter.Error($"File or directory does not exist: {_fileOrDirectory}");
                return;
            }

            // Trim filelist to only include HD container files
            allFiles = allFiles.Where(file => file.IsAzwResOrAzw6File()).ToList();

            _totalBooks = azwFiles.Count;

            ConversionBegin();

            await ConvertAzwFilesAndHdContainersAsync(azwFiles, allFiles);

            ConversionEnd(azwFiles.Count);
        }

        private async Task ConvertAzwFilesAndHdContainersAsync(List<FileInfo> azwFiles, List<FileInfo> hdContainerFiles)
        {
            var hdStreamMap = await AnalyzeHdContainersAsync(hdContainerFiles);

            foreach (var azwFile in azwFiles)
            {
                var metadata = MetadataManager.GetConfiguredMetadata();

                using var stream = AsyncStreams.AsyncFileReadStream(azwFile.FullName);

                await metadata.ReadMetadataAsync(stream);
                await metadata.ReadImageRecordsAsync();

                var cbzFile = GetCbzFile(azwFile.FullName, metadata.MobiHeader.FullName);
                var coverFile = GetCoverFile(cbzFile);

                var dataLen = azwFile.Length;

                if (hdStreamMap.TryGetValue(metadata.MobiHeader.FullName, out var hdStream))
                {
                    dataLen += hdStream.Length;

                    hdStream.Position = 0; // Reset so we can read the full metadata
                    await metadata.ReadHDImageRecordsAsync(hdStream);
                }

                CbzState state = null!;

                switch (Action)
                {
                    case CbzMageAction.AzwConvert:
                        {
                            var convertEngine = new ConvertEngine();
                            state = await convertEngine.ConvertMetadataAsync(metadata, cbzFile, dataLen, coverFile);
                            break;
                        }
                    case CbzMageAction.AzwScan:
                        {
                            var scanEngine = new ScanEngine();
                            state = await ScanEngine.ReadCbzStateAsync(metadata.PageRecordsHD, metadata.PageRecords);
                            break;
                        }
                }

                hdStream?.Dispose();

                PrintCbzState(cbzFile, state);
            }
        }

        private static string GetCbzFile(string azwFile, string title)
        {
            title = $"{title.ToFileSystemString()}.cbz";

            if (!string.IsNullOrEmpty(Settings.CbzDir) && !Settings.CbzDirSetBySystem)
            {
                return Path.Combine(Settings.CbzDir, title);
            }

            var dir = Path.GetDirectoryName(azwFile);
            return Path.Combine(dir, title);
        }

        private static string GetCoverFile(string cbzFile)
        {
            return Settings.SaveCover ? Path.ChangeExtension(cbzFile, ".jpg") : null;
        }

        private static async Task<Dictionary<string, FileStream>> AnalyzeHdContainersAsync(List<FileInfo> hdContainerFiles)
        {
            var hdStreamMap = new Dictionary<string, FileStream>();

            foreach (var hdContainerFile in hdContainerFiles)
            {
                var hdStream = AsyncStreams.AsyncFileReadStream(hdContainerFile.FullName);

                var pdbHeader = new PDBHead(skipProperties: true, skipRecords: true);
                await pdbHeader.ReadHeaderAsync(hdStream);

                var azw6Header = new Azw6Head(skipExthHeader: true);
                await azw6Header.ReadHeaderAsync(hdStream);

                hdStreamMap.Add(azw6Header.Title, hdStream);
            }

            return hdStreamMap;
        }
    }
}
