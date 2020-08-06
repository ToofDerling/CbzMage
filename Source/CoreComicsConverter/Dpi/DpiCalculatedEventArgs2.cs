using System;

namespace CoreComicsConverter.Dpi
{
    public class DpiCalculatedEventArgs2 : EventArgs
    {
        public DpiCalculatedEventArgs2(int dpi, int requiredDpi, (int width, int height) imageSize)
        {
            Dpi = dpi;

            MinimumDpi = requiredDpi;

            ImageSize = imageSize;
        }

        public int Dpi { get; private set; }

        public int MinimumDpi { get; set; }

        public (int width, int height) ImageSize { get; private set; }
    }
}
