using CoreComicsConverter.Model;
using System;

namespace CoreComicsConverter
{
    public class PageEventArgs : EventArgs
    {
        public PageEventArgs(Page page)
        {
            Page = page;
        }

        public Page Page { get; private set; }
    }
}
