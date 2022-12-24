using CbzMage.Shared.ManagedBuffers;

namespace PdfConverter.Ghostscript
{
    public interface IImageDataHandler
    {
        void HandleImageData(ManagedBuffer image);
    }
}
