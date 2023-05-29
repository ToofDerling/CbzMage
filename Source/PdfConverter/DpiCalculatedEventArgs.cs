namespace PdfConverter
{
    public class DpiCalculatedEventArgs : EventArgs
    {
        public DpiCalculatedEventArgs(int dpi, int width, int height)
        {
            Dpi = dpi;

            Width = width;

            Height = height;
        }

        public int Dpi { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }
    }
}
