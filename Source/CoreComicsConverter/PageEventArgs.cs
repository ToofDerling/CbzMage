using CoreComicsConverter.Model;
using System;

namespace CoreComicsConverter
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
