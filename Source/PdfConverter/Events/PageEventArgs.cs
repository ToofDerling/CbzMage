using PdfConverter.Model;
using System;

namespace PdfConverter.Events
{
    public class PageEventArgs : EventArgs
    {
        public PageEventArgs(ComicPage page)
        {
            Page = page;
        }

        public ComicPage Page { get; private set; }
    }
}
