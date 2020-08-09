using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;

namespace CoreComicsConverter
{
    public interface IPageParser
    {
        public event EventHandler<PageEventArgs> PageParsed;

        public List<Page> ParsePages();
    }
}
