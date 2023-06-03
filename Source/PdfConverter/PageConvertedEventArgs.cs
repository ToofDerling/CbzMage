namespace PdfConverter
{
    public class PageConvertedEventArgs : EventArgs
    {
        public PageConvertedEventArgs(int pageNumber)
        {
            PageNumber = pageNumber;
        }

        public int PageNumber { get; private set; }
    }
}
