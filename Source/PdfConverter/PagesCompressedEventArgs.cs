namespace PdfConverter
{
    public class PagesCompressedEventArgs : EventArgs
    {
        public PagesCompressedEventArgs(IEnumerable<string> pages)
        {
            Pages = pages;
        }

        public IEnumerable<string> Pages { get; private set; }
    }
}
