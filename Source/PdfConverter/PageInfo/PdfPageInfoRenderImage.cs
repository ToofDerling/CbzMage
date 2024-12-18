using CbzMage.Shared.Buffers;

namespace PdfConverter.PageInfo
{
    public class PdfPageInfoRenderImage : AbstractPdfPageInfo
    {
        public PdfPageInfoRenderImage(int pageNumber) : base(pageNumber)
        {
        }
    
        public ArrayPoolBufferWriter<byte> ImageData { get; set; }

        public int Dpi { get; set; }
    }
}
