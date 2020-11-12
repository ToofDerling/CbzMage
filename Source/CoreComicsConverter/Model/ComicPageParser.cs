using CoreComicsConverter.Events;
using CoreComicsConverter.Helpers;
using System;
using System.Collections.Generic;

namespace CoreComicsConverter.Model
{
    public abstract class ComicPageParser
    {
        public abstract event EventHandler<PageEventArgs> PageParsed;

        protected abstract List<ComicPage> Parse();

        public List<ComicPage> ParseImages(Comic comic)
        {
            if (comic.PageCount == 0)
            {
                throw new ApplicationException("Comic pageCount is 0");
            }

            var progressReporter = new ProgressReporter(comic.PageCount);
            PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page {e.Page.Number}");

            var pageSizes = Parse();

            progressReporter.EndProgress();

            ProgressReporter.Info($" {comic.PageCount} pages {comic.ImageCount} images");

            return pageSizes;
        }
    }
}
