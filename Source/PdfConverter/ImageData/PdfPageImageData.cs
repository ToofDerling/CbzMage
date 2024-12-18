using CbzMage.Shared.Buffers;
using CbzMage.Shared.IO;
using PdfConverter.PageInfo;

namespace PdfConverter.ImageData
{
    internal class PdfPageImageData
    {
        public static async Task CopyToStreamAsync(AbstractPdfPageInfo imageInfo, Stream stream)
        {
            if (imageInfo is PdfPageInfoRenderImage renderImageInfo)
            {
                var imageData = renderImageInfo.ImageData.WrittenMemory;
                await stream.WriteAsync(imageData);
            }
            else if (imageInfo is PdfPageInfoSaveImage saveImageInfo)
            {
                using var imageStream = AsyncStreams.AsyncFileReadStream(saveImageInfo.SavedImagePath);
                await imageStream.CopyToAsync(stream);
            }
            else
            {
                throw new ArgumentNullException(nameof(imageInfo));
            }
        }

        public static ArrayPoolBufferWriter<byte> GetRenderedImageData(AbstractPdfPageInfo imageInfo)
        {
            if (imageInfo is PdfPageInfoRenderImage renderImageInfo)
            {
                return renderImageInfo.ImageData;
            }
            else
            {
                throw new ArgumentNullException(nameof(imageInfo));
            }
        }
    }
}
