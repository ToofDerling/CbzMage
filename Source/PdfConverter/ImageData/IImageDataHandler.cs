using CbzMage.Shared.Buffers;

namespace PdfConverter.ImageData
{
    public interface IImageDataHandler
    {
        // From pngtoppm -png
        void HandleRenderedImageData(ArrayPoolBufferWriter<byte> image);

        // From pdfimages
        void HandleSavedImageData(ArrayPoolBufferWriter<byte> image, string imageExt);
    }
}
