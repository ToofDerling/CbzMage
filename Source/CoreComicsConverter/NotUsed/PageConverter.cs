using ImageMagick;
using System.IO;

namespace CoreComicsConverter
{
    public class PageConverter
    {
        public string ConvertPage(string pngPath, string jpgPath)
        {
            using var image = new MagickImage(pngPath)
            {
                Format = MagickFormat.Jpg,
                Interlace = Interlace.Plane,
                Quality = Settings.JpegQuality
            };

            image.Write(jpgPath);
            File.Delete(pngPath);

            return Path.GetFileName(jpgPath);
        }
    }
}
