using ImageMagick;

namespace PdfConverter.Ghostscript
{
    public interface IPipedImageDataHandler
    {
        void HandleImageData(MagickImage image);
    }
}
