using System.IO.Compression;

namespace BlackSteedConverter
{
    public sealed class Settings
    {
        // All properties with a public setter are read from settings file

        public static string CbzDir { get; set; }

        public static bool SaveCover { get; set; }

        public static CompressionLevel CompressionLevel { get; set; }
    }
}
