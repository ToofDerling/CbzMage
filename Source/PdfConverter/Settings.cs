using System.IO.Compression;

namespace PdfConverter
{
    public class Settings
    {
        // All properties with a public setter are read from settings file

        public static int MinimumDpi { get; set; }

        public static int MinimumHeight { get; set; }

        public static int MaximumHeight { get; set; }

        public static int JpgQuality { get; set; }

        public static int GhostscriptReaderThreads { get; set; }

        public static PdfConverterMode PdfConverterMode { get; set; }

        public static int BufferSize { get; set; }

        public static CompressionLevel CompressionLevel { get; set; }

        public static int GhostscriptMinVersion { get; set; }
    }
}
