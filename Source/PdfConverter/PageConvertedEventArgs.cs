namespace PdfConverter
{
    public class PageConvertedEventArgs : EventArgs
    {
        public PageConvertedEventArgs(string page)
        {
            Page = page;
        }

        public string Page { get; private set; }
    }
}
