using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;

namespace CoreComicsConverter.Images
{
    public abstract class ImageParser : IDisposable
    {
        protected Comic _comic;

        protected List<string> _parserWarnings;

        protected ImageParser(Comic comic)
        {
            _comic = comic;
            _parserWarnings = new List<string>();
        }

        public List<string> GetParserWarnings()
        {
            return _parserWarnings;
        }

        public abstract event EventHandler<PageEventArgs> PageParsed;

        public abstract void OpenComicSetPageCount();

        public abstract List<Page> ParsePagesSetImageCount();

        public virtual void Dispose()
        {
            // nop
        }
    }
}
