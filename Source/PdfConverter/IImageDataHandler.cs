using CbzMage.Shared.Buffers;

namespace PdfConverter
{
    public interface IImageDataHandler
    {
        // From Ghostscript
        void HandleRenderedImageData(ArrayPoolBufferWriter<byte> image);

        // From IText
        void HandleSavedImageData(ArrayPoolBufferWriter<byte> image, string imageExt);
    }
}
