using System.IO.Compression;

namespace AzwConverter
{
    public sealed class Settings
    {
        // All properties with a public setter are read from settings file

        public static string[] TrimPublishers { get; set; }

        public static string AzwDir { get; set; }

        public static string TitlesDir { get; set; }

        public static string AnalysisDir { get; set; }

        public static string ConvertedTitlesDirName { get; set; }

        public static string ConvertedTitlesDir { get; private set; }

        public static void SetConvertedTitlesDir(string dir)
        {
            ConvertedTitlesDir = dir;
        }

        public static string CbzDir { get; set; }

        public static bool SaveCover { get; set; }
        public static bool SaveCoverOnly { get; set; }
        public static string? SaveCoverDir { get; set; }

        public static int NumberOfThreads { get; set; }

        public static ParallelOptions ParallelOptions { get; private set; }

        public static void SetParallelOptions(ParallelOptions parallelOptions)
        {
            ParallelOptions = parallelOptions;
        }

        public static CompressionLevel CompressionLevel { get; set; }

        public static string NewTitleMarker { get; set; }
        public static string UpdatedTitleMarker { get; set; }

        public static string[] AllMarkers { get; private set; }

        public static void SetAllMarkers()
        {
            AllMarkers = new string[] { NewTitleMarker, UpdatedTitleMarker };
        }

        private static readonly string[] azwExts = new[] { ".azw", ".mbpV2", ".azw.res" };

        public static string AzwExt => azwExts[0];
        public static string AzwResExt => azwExts[2];
    }
}
