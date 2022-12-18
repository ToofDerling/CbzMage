using CbzMage.Shared.ManagedBuffers;

namespace PdfConverter.Ghostscript
{
    public interface IPipedImageDataHandler
    {
        void HandleImageData(ManagedBuffer image);
    }
}
