using ImageMagick;
using System.IO;

namespace Rotvel.PdfConverter
{
    public class PageConverter
    {
        public string ConvertPage(string pngPath, string jpgPath)
        {
            using var image = new MagickImage(pngPath)
            {
                Format = MagickFormat.Jpg,
                Interlace = Interlace.Plane,
                Quality = Program.QualityConstants.JpegQuality
            };

            //if (image.Height > Program.QualityConstants.MaxHeightThreshold)
            //{
            //    image.Resize(new MagickGeometry
            //    {
            //        Greater = true,
            //        Less = false,
            //        Height = Program.QualityConstants.MaxHeight
            //    });
            //}

            image.Write(jpgPath);
            File.Delete(pngPath);

            return Path.GetFileName(jpgPath);
        }
    }
}
