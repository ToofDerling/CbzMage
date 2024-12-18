namespace PdfConverter
{
    public enum ParserMode
    { 
        Images, Text
    }

    public class PageParsedEventArgs : EventArgs
    {
        public PageParsedEventArgs(int currentPage, ParserMode parserMode)
        {
            CurrentPage = currentPage;

            ParserMode = parserMode;
        }

        public int CurrentPage { get; private set; }

        public ParserMode ParserMode { get; set; }
    }
}
