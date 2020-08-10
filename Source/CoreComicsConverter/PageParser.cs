using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;

namespace CoreComicsConverter
{
    public abstract class PageParser
    {
        public abstract event EventHandler<PageEventArgs> PageParsed;

        protected abstract List<ComicPage> Parse();

        public List<ComicPage> ParseImages(Comic comic)
        {
            if (comic.PageCount == 0)
            {
                throw new ApplicationException("Comic pageCount is 0");
            }
            Console.WriteLine($"{comic.PageCount} pages");

            var progressReporter = new ProgressReporter(comic.PageCount);
            PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page {e.Page.Number}");

            var pageSizes = Parse();

            Console.WriteLine();
            Console.WriteLine($"{comic.ImageCount} images");

            return pageSizes;
        }
    }
}
