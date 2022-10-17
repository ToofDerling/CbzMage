namespace PdfConverter
{
    public class Settings
    {
        public static int MinimumDpi => 300;

        public static int MinimumHeight => 1920;

        public static int MaximumHeight => MinimumHeight * 2;

        public static int JpegQuality => 95;

        public static int ThreadCount => 4;

        public static int ResizeSlack => 100;
    }
}
