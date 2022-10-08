using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace AzwConverter
{
    public class Settings
    {
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
            var titlesDir = config.GetValue<string>("TitlesDir");
            if (string.IsNullOrWhiteSpace(titlesDir))
            {
                var dir = new DirectoryInfo(AzwDir).Parent;
                titlesDir = Path.Combine(dir.FullName, "Titles");
            }
            titlesDir.CreateDirIfNotExists();
            TitlesDir = titlesDir;

            //ConvertedTitlesDirName
            var convertedTitlesDirName = config.GetValue<string>("ConvertedTitlesDirName");
            if (string.IsNullOrWhiteSpace(convertedTitlesDirName))
            {
                convertedTitlesDirName = "Converted Titles";
            }
            ConvertedTitlesDir = Path.Combine(TitlesDir, convertedTitlesDirName);
            ConvertedTitlesDir.CreateDirIfNotExists();

            //CbzDir
            var cbzDir = config.GetValue<string>("CbzDir");
            if (string.IsNullOrWhiteSpace(cbzDir))
            {
                var dir = new DirectoryInfo(AzwDir).Parent;
                cbzDir = Path.Combine(dir.FullName, "Cbz Files");
            }
            cbzDir.CreateDirIfNotExists();
            CbzDir = cbzDir;

            //NewTitleMarker
            var newTitleMarker = config.GetValue<string>("NewTitleMarker");
            if (string.IsNullOrWhiteSpace(newTitleMarker))
            {
                newTitleMarker = ".NEW";
            }
            NewTitleMarker = newTitleMarker;

            //UpdatedTitleMarker
            var updatedTitleMarker = config.GetValue<string>("UpdatedTitleMarker");
            if (string.IsNullOrWhiteSpace(updatedTitleMarker))
            {
                updatedTitleMarker = ".UPDATED";
            }
            UpdatedTitleMarker = updatedTitleMarker;

            //TrimPublishers
            var trimPublishers = config.GetSection("TrimPublishers").Get<string[]>();
            TrimPublishers = trimPublishers ?? Array.Empty<string>();

            //NumberOfThreads
            var numberOfThreads = config.GetValue<int>("NumberOfThreads");
            if (numberOfThreads == default)
            {
                numberOfThreads = 3;
            }
            NumberOfThreads = numberOfThreads;

            //CompressionLevel
            var compressionLevel = config.GetValue<CompressionLevel>("CompressionLevel");
            CompressionLevel = compressionLevel;
        }

        public static string[] TrimPublishers { get; private set; }

        public static string AzwDir { get; private set; }
        //B08KBRZ9TF_EBOK.mbpV2
        //B08KBRZ9TF_EBOK.azw
        //CR!TS8MDDF7C973N6VNGHX5JW0VQGJX.azw.res
        private static readonly string[] azwExts = new[] { ".azw", ".mbpV2", ".azw.res" };
        public static string AzwExt => azwExts[0];
        public static string AzwResExt => azwExts[2];

        public static string TitlesDir { get; private set; }
        public static string ConvertedTitlesDir { get; private set; }

        public static string CbzDir { get; private set; }

        // Tests shows this is a good value for a HDD. Need to test an SSD as well. 
        public static int NumberOfThreads { get; private set; }

        public static CompressionLevel CompressionLevel { get; private set; }

        public static string NewTitleMarker { get; private set; }
        public static string UpdatedTitleMarker { get; private set; }

        public static string[] AllMarkers => new string[] { NewTitleMarker, UpdatedTitleMarker };
    }
}
