namespace PdfConverter
{
    public class PagesCompressedEventArgs : EventArgs
    {
        public PagesCompressedEventArgs(IEnumerable<int> pages)
        {
            Pages = pages;
        }

        public IEnumerable<int> Pages { get; private set; }
    }
}
