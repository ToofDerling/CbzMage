using System.IO.Compression;

namespace PdfConverter
{
    public sealed class Settings
    {
        // All properties with a public setter are read from settings file

        public static string CbzDir { get; set; }

        public static bool SaveCover { get; set; }
        /// <summary>
        /// If this is true SaveCover is also true
        /// </summary>
        public static bool SaveCoverOnly { get; set; }
        public static string? SaveCoverDir { get; set; }

        public static int MinimumDpi { get; set; }

        public static int MinimumHeight { get; set; }

        public static int MaximumHeight { get; set; }

        public static int JpgQuality { get; set; }

        public static int NumberOfThreads { get; set; }

        public static CompressionLevel CompressionLevel { get; set; }

        public static int ImageBufferSize => 4194304 * 2;

        public static Version GhostscriptMinVersion => new(10, 0);

        public static string GhostscriptPath { get; set; }

        public static Version GhostscriptVersion { get; private set; }

        public static void SetGhostscriptVersion(Version version)
        {
            GhostscriptVersion = version;
        }

        public static int WriteBufferSize => 262144;

        public static string ScanAllDirectoriesPattern => $"{Path.DirectorySeparatorChar}**";
    }
}
