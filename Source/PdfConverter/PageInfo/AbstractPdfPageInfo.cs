namespace PdfConverter.PageInfo
{
    public abstract class AbstractPdfPageInfo
    {
        public int PageNumber { get; private set; }

        public AbstractPdfPageInfo(int pageNumber)
        {
            PageNumber = pageNumber;
        }

        public (int width, int height) LargestImage { get; set; }

        public string LargestImageExt { get; set; }

        public (int width, int height) PageSize { get; set; }

        public int ImageCount { get; set; }

        public int? ResizeHeight { get; set; }

        public static bool UseResizeHeight(int height, int? resizeHeight)
        {
            return resizeHeight.HasValue && (resizeHeight < (height * 1.1d)) || (resizeHeight > (height * 0.9d));
        }
    }
}
