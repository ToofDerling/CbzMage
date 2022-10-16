using System;

namespace PdfConverter
{
    public class DpiCalculatedEventArgs : EventArgs
    {
        public DpiCalculatedEventArgs(int dpi, int requiredDpi, int width)
        {
            Dpi = dpi;

            MinimumDpi = requiredDpi;

            Width = width;
        }

        public int Dpi { get; private set; }

        public int MinimumDpi { get; set; }

        public int Width { get; private set; }
    }
}
