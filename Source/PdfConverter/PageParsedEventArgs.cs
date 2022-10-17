namespace PdfConverter
{
    public class PageParsedEventArgs : EventArgs
    {
        public PageParsedEventArgs(int currentPage)
        {
            CurrentPage = currentPage;
        }

        public int CurrentPage { get; private set; }
    }
}
