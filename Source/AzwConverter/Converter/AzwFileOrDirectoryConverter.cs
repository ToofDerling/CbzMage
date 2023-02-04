using AzwConverter.Engine;
using CbzMage.Shared;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.IO;
using MobiMetadata;
using System.Collections.Concurrent;

namespace AzwConverter.Converter
{
    public class AzwFileOrDirectoryConverter : BaseAzwConverter
    {
        private string _fileOrDirectory;

        public AzwFileOrDirectoryConverter(CbzMageAction action, string fileOrDirectory)
            : base(action)
        {
            _fileOrDirectory = fileOrDirectory;

            if (!string.IsNullOrEmpty(Settings.CbzDir) && !Settings.CbzDirSetBySystem)
            {
                ProgressReporter.Info($"Cbz backups: {Settings.CbzDir}");
                ProgressReporter.Line();
            }
            ProgressReporter.Info($"Conversion threads: {Settings.NumberOfThreads}");
            ProgressReporter.Info($"Cbz compression: {Settings.CompressionLevel}");
        }

        public async Task ConvertOrScanAsync()
        {
            if (string.IsNullOrEmpty(_fileOrDirectory))
            {
                throw new ArgumentNullException(nameof(_fileOrDirectory));
            }

            // Must run before before the checks for file/dir existance
            _fileOrDirectory = SharedSettings.GetDirectorySearchOption(_fileOrDirectory, out var searchOption);

            var azwFiles = new List<FileInfo>();
            var allFiles = new List<FileInfo>();

            if (File.Exists(_fileOrDirectory))
            {
                var fileInfo = new FileInfo(_fileOrDirectory);

                if (!fileInfo.IsAzwOrAzw3File())
                {
                    ProgressReporter.Error($"Not an azw or azw3 file [{_fileOrDirectory}]");
                    return;
                }

                azwFiles.Add(fileInfo);

                var directory = fileInfo.Directory;
                if (directory == null)
                {
                    ProgressReporter.Error($"Error retrieving directory of [{_fileOrDirectory}]");
                    return;
                }

                allFiles.AddRange(directory.GetFiles());
            }
            else if (Directory.Exists(_fileOrDirectory))
            {
                var directoryInfo = new DirectoryInfo(_fileOrDirectory);
                allFiles.AddRange(directoryInfo.GetFiles("*", searchOption));

                azwFiles = allFiles.Where(file => file.IsAzwOrAzw3File()).ToList();
                if (azwFiles.Count == 0)
                {
                    ProgressReporter.Error($"No azw or azw3 files found in [{_fileOrDirectory}]");
                    return;
                }
            }
            else
            {
                ProgressReporter.Error($"File or directory does not exist [{_fileOrDirectory}]");
                return;
            }

            await DoConvertAzwFilesAndHDContainersAsync(azwFiles, allFiles);
        }

        private async Task DoConvertAzwFilesAndHDContainersAsync(List<FileInfo> azwFiles,
            List<FileInfo> allFiles)
        {
            var hdContainerFiles = allFiles.Where(file => file.IsAzwResOrAzw6File()).ToList();

            var actionString = Action == CbzMageAction.AzwConvert ? "Converting" : "Listing";

            ProgressReporter.Line();
            ProgressReporter.Info($"{actionString} {azwFiles.Count} azw/azw3 file{azwFiles.SIf1()}");
            if (hdContainerFiles.Count > 0)
            {
                ProgressReporter.Info($"Found {hdContainerFiles.Count} azw.res/azw6 file{hdContainerFiles.SIf1()} with HD images");
            }

            _totalBooks = azwFiles.Count;

            ConversionBegin();

            var hdContainerHeaders = await AnalyzeHdContainersAsync(hdContainerFiles);
            await ConvertAzwFilesAndHdContainersAsync(azwFiles, hdContainerHeaders);

            ConversionEnd(azwFiles.Count);
        }

        private async Task ConvertAzwFilesAndHdContainersAsync(List<FileInfo> azwFiles,
            List<Azw6Head> hdContainerHeaders)
        {
            await Parallel.ForEachAsync(azwFiles, Settings.ParallelOptions,
                async (azwFile, _) =>
                {
                    CbzState? state = null;

                    switch (Action)
                    {
                        case CbzMageAction.AzwConvert:
                            {
                                if (Settings.SaveCoverOnly)
                                {
                                    var saveConverEngine = new SaveFileCoverEngine();
                                    await saveConverEngine.SaveFileCoverAsync(azwFile, hdContainerHeaders);

                                    PrintCoverString(saveConverEngine.GetCoverFile(), saveConverEngine.GetCoverString());
                                }
                                else
                                {
                                    var convertEngine = new ConvertFileEngine();
                                    state = await convertEngine.ConvertFileAsync(azwFile, hdContainerHeaders);

                                    PrintCbzState(state!.Name, state);
                                }
                                break;
                            }
                        case CbzMageAction.AzwScan:
                            {
                                var scanEngine = new ScanFileEngine();
                                state = await scanEngine.ScanFileAsync(azwFile, hdContainerHeaders);

                                PrintCbzState(state!.Name, state);
                                break;
                            }
                    }
                });
        }

        private static async Task<List<Azw6Head>> AnalyzeHdContainersAsync(List<FileInfo> hdContainerFiles)
        {
            if (hdContainerFiles.Count == 0)
            {
                return new List<Azw6Head>();
            }

            var hdContainerMap = new ConcurrentDictionary<string, Azw6Head>();

            await Parallel.ForEachAsync(hdContainerFiles, Settings.ParallelOptions,
                async (hdContainerFile, _) =>
                {
                    using var hdStream = AsyncStreams.AsyncFileReadStream(hdContainerFile.FullName);

                    var pdbHeader = new PDBHead(skipProperties: false, skipRecords: false);
                    await pdbHeader.ReadHeaderAsync(hdStream).ConfigureAwait(false);

                    var azw6Header = new Azw6Head(skipExthHeader: false);
                    await azw6Header.ReadHeaderAsync(hdStream).ConfigureAwait(false);

                    azw6Header.Path = hdContainerFile;
                    hdContainerMap[hdContainerFile.FullName] = azw6Header;
                });

            return hdContainerMap.Values.ToList();
        }
    }
}
