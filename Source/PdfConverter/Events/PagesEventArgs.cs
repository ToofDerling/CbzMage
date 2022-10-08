using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;

namespace CoreComicsConverter.Events
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
