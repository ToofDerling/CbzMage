namespace PdfConverter.PageInfo
{
    public class PdfPageInfoSaveImage : AbstractPdfPageInfo
    {
        public PdfPageInfoSaveImage(int pageNumber) : base(pageNumber)
        {
        }

        public string SavedImagePath { get; set; }
        
        public bool IsJpg() => LargestImageExt == PdfImageExt.Jpg;
    }
}
