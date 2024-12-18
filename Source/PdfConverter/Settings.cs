using System.IO.Compression;

namespace PdfConverter
{
    public sealed class Settings
    {
        // All properties with a public setter are read from settings file

        public static string PdfToPngPath { get; set; }

        public static void SetPdfToPngPath(string pdfToPngPath)
        {
            PdfToPngPath = pdfToPngPath;
        }

        public static string PdfImagesPath { get; set; }

        public static void SetPdfImagesPath(string pdfImagesPath)
        {
            PdfImagesPath = pdfImagesPath;
        }


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

        public static bool ForceJpgImages { get; set; }

        public static int NumberOfThreads { get; set; }

        public static CompressionLevel CompressionLevel { get; set; }

        public static int ImageBufferSize => 8 * 1048576;

        public static int ReadRequestSize => 32 * 1024;

  
    }
}
