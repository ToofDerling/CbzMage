using PdfConverter.Model;
using System;
using System.Collections.Generic;

namespace PdfConverter.Events
{
    public class PagesEventArgs : EventArgs
    {
        public PagesEventArgs(IReadOnlyCollection<ComicPage> pages)
        {
            Pages = pages;
        }

        public IReadOnlyCollection<ComicPage> Pages { get; private set; }
    }
}
