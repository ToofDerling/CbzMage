using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace AzwConverter
{
    public class Settings
    {
        // For testing. If >0 overrules the NumberOfThreads setting.
        private const int maxThreads = 0;

        // Defaults
        private const string defaultTitlesDir = "Titles";
        private const string defaultCbzDir = "Cbz Backups";
        
        private const bool defaultSaveCover = false;
        private const bool defaultSaveCoverOnly = false;

        private const string defaultConvertedTitlesDirName = "Converted Titles";
        private const string defaultNewTitleMarker = ".NEW";
        private const string defaultUpdateTitleMarker = ".UPDATED";

        private const int defaultNumberOfThreads = 3;
        private const CompressionLevel defaultCompressionLevel = CompressionLevel.Fastest;

        public static void ReadAppSettings(IConfiguration config)
        {
            //AzwDir
            var azwDir = config.GetValue<string>("AzwDir");
            if (string.IsNullOrWhiteSpace(azwDir) || !Directory.Exists(azwDir))
            {
                throw new Exception("Must configure AzwDir in appsettings.json");
            }
            AzwDir = azwDir;

            //TitlesDir
            var titlesDir = config.GetValue("TitlesDir", defaultTitlesDir);
            if (string.IsNullOrWhiteSpace(titlesDir))
            {
                var dir = new DirectoryInfo(AzwDir).Parent;
                titlesDir = Path.Combine(dir.FullName, defaultTitlesDir);
            }
            titlesDir.CreateDirIfNotExists();
            TitlesDir = titlesDir;

            //SaveCover
            SaveCover = config.GetValue("SaveCover", defaultSaveCover);
            SaveCoverOnly = config.GetValue("SaveCoverOnly", defaultSaveCoverOnly);

            //SaveCoverDir
            if (SaveCover)
            {
                var saveCoverDir = config.GetValue("SaveCoverDir", string.Empty);
                if (!string.IsNullOrWhiteSpace(saveCoverDir))
                {
                    saveCoverDir.CreateDirIfNotExists();
                    SaveCoverDir = saveCoverDir;
                }
                else 
                {
                    SaveCoverDir = null;
                }
            }

            //ConvertedTitlesDirName
            var convertedTitlesDirName = config.GetValue("ConvertedTitlesDirName", defaultConvertedTitlesDirName);
            if (string.IsNullOrWhiteSpace(convertedTitlesDirName))
            {
                convertedTitlesDirName = defaultConvertedTitlesDirName;
            }
            ConvertedTitlesDir = Path.Combine(TitlesDir, convertedTitlesDirName);
            ConvertedTitlesDir.CreateDirIfNotExists();

            //CbzDir
            var cbzDir = config.GetValue("CbzDir", defaultCbzDir);
            if (string.IsNullOrWhiteSpace(cbzDir))
            {
                var dir = new DirectoryInfo(AzwDir).Parent;
                cbzDir = Path.Combine(dir.FullName, defaultCbzDir);
            }
            cbzDir.CreateDirIfNotExists();
            CbzDir = cbzDir;

            //NewTitleMarker
            var newTitleMarker = config.GetValue("NewTitleMarker", defaultNewTitleMarker);
            NewTitleMarker = string.IsNullOrWhiteSpace(newTitleMarker) ? defaultNewTitleMarker : newTitleMarker; 

            //UpdatedTitleMarker
            var updatedTitleMarker = config.GetValue("UpdatedTitleMarker", defaultUpdateTitleMarker);
            UpdatedTitleMarker = string.IsNullOrWhiteSpace(updatedTitleMarker) ? defaultUpdateTitleMarker : updatedTitleMarker;

            //TrimPublishers
            var trimPublishers = config.GetSection("TrimPublishers").Get<string[]>();
            TrimPublishers = trimPublishers ?? Array.Empty<string>();

            //NumberOfThreads
            numberOfThreads = config.GetValue("NumberOfThreads", defaultNumberOfThreads);

            //CompressionLevel
            CompressionLevel = config.GetValue("CompressionLevel", defaultCompressionLevel);
        }

        public static string[] TrimPublishers { get; private set; }

        public static string AzwDir { get; private set; }

        private static readonly string[] azwExts = new[] { ".azw", ".mbpV2", ".azw.res" };
        public static string AzwExt => azwExts[0];
        public static string AzwResExt => azwExts[2];

        public static string TitlesDir { get; private set; }
        public static string ConvertedTitlesDir { get; private set; }

        public static string CbzDir { get; private set; }

        public static bool SaveCover { get; private set; }
        public static bool SaveCoverOnly { get; private set; }
        public static string? SaveCoverDir { get; private set; }

        private static int numberOfThreads;

        public static ParallelOptions ParallelOptions => GetParallelOptions();

        private static ParallelOptions GetParallelOptions()
        {
            var numThreads = Math.Min(numberOfThreads, Environment.ProcessorCount);
            if (maxThreads > 0)
            {
                numThreads = maxThreads;
            }
            return new ParallelOptions { MaxDegreeOfParallelism = numThreads };
        }

        public static CompressionLevel CompressionLevel { get; private set; }

        public static string NewTitleMarker { get; private set; }
        public static string UpdatedTitleMarker { get; private set; }

        public static string[] AllMarkers => new string[] { NewTitleMarker, UpdatedTitleMarker };
    }
}
