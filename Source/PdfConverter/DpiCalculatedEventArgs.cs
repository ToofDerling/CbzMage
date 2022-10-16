namespace PdfConverter
{
    public class DpiCalculatedEventArgs : EventArgs
    {
        public DpiCalculatedEventArgs(int dpi, int requiredDpi, int width, int height)
        {
            Dpi = dpi;

            MinimumDpi = requiredDpi;

            Width = width;

            Height = height;
        }

        public int Dpi { get; private set; }

        public int MinimumDpi { get; set; }

        public int Width { get; private set; }
    
        public int Height { get; private set; } 
    }
}
