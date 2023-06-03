using CbzMage.Shared.Buffers;

namespace PdfConverter
{
    public interface IImageDataHandler
    {
        void HandleParsedImageData(ArrayPoolBufferWriter<byte> image);

        void HandleSavedImageData(ArrayPoolBufferWriter<byte> image, string imageExt);
    }
}
