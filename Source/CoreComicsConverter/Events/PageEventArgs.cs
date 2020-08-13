using CoreComicsConverter.Model;
using System;

namespace CoreComicsConverter.Events
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
