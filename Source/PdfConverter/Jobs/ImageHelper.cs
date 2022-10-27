using ImageMagick;

namespace PdfConverter.Jobs
{
    internal class ImageHelper
    {
        public static bool ConvertJpg(MagickImage image, int? resizeHeight)
        {
            image.Format = MagickFormat.Jpg;
            image.Interlace = Interlace.Plane;
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
