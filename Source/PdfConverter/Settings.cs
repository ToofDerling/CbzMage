namespace PdfConverter
{
    public class Settings
    {
        public static int MinimumDpi => 300;

        public static int MinimumHeight => 1920;

        public static int MaximumHeight => MinimumHeight * 2;

        public static int JpegQuality => 95;

        public static int ThreadCount => Math.Max(1, (Environment.ProcessorCount / 2) - 2);

        public static int BufferSize => 20000000;

        public static Mode Mode => Mode.Faster;
    }
}
