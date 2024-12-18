using PdfConverter.PageInfo;

namespace PdfConverter.ImageData
{
    public interface IImageDataHandler
    {
        void HandleImageData(AbstractPdfPageInfo pageInfo);
    }
}
