using ImageMagick;

namespace PdfConverter.Jobs
{
    internal class ImageHelper
    {
        public static bool ConvertJpg(MagickImage image, int? resizeHeight)
        {
            // Produce baseline jpgs with no subsampling.

            image.Format = MagickFormat.Jpg;
            image.Quality = Settings.JpgQuality;

            var resize = false;
            if (resizeHeight.HasValue && image.Height > resizeHeight.Value)
            {
                resize = true;

                image.Resize(new MagickGeometry
                {
                    Greater = true,
                    Less = false,
                    Height = resizeHeight.Value
                });
            }

            return resize;
        }
    }
}
