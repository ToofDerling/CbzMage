using CbzMage.Shared.Buffers;

namespace PdfConverter.Ghostscript
{
    public interface IImageDataHandler
    {
        void HandleImageData(ByteArrayBufferWriter image);
    }
}
