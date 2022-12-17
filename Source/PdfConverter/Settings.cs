using System.IO.Compression;

namespace PdfConverter
{
    public sealed class Settings
    {
        // All properties with a public setter are read from settings file

        public static int MinimumDpi { get; set; }

        public static int MinimumHeight { get; set; }

        public static int MaximumHeight { get; set; }

        public static int JpgQuality { get; set; }

        public static int GhostscriptReaderThreads { get; set; }

        public static CompressionLevel CompressionLevel { get; set; }

        public static int BufferSize => 4194304;

        public static int GhostscriptMinVersion => 10;

        public static string GhostscriptPath { get; set; }

        public static int BufferRemainingThreshold => 262144;

        public static int PipeBufferSize => 262144;
    }
}
