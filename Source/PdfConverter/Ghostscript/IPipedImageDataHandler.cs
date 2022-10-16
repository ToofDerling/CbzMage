using PdfConverter.ManagedBuffers;

namespace PdfConverter.Ghostscript
{
    public interface IPipedImageDataHandler
    {
        void HandleImageData(ManagedBuffer imageData);
    }
}
