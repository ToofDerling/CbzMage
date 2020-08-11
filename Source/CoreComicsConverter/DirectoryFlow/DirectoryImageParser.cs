using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreComicsConverter.DirectoryFlow
{
    public class DirectoryImageParser : ComicPageParser
    {
        private ConcurrentQueue<ComicPage> _pageQueue;

        private readonly DirectoryComic _comic;

        public DirectoryImageParser(DirectoryComic comic)
        {
            _comic = comic;

            var sortedFiles = comic.Files.OrderBy(path => path.ToString());

            _pageQueue = new ConcurrentQueue<ComicPage>();
            var pageNumber = 1;

            foreach (var file in sortedFiles)
            {
                _pageQueue.Enqueue(new ComicPage { Number = pageNumber, Path = file });
                pageNumber++;
            }

            _comic.PageCount = _pageQueue.Count;
        }

        public override event EventHandler<PageEventArgs> PageParsed;

        protected override List<ComicPage> Parse()
        {
            var pageSizes = new ConcurrentBag<ComicPage>();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (!_pageQueue.IsEmpty)
                {
                    if (_pageQueue.TryDequeue(out var page))
                    {
                        page.Ping();
                        pageSizes.Add(page);

                        PageParsed?.Invoke(this, new PageEventArgs(page));
                    }
                }
            });

            var pageList = pageSizes.OrderBy(p => p.Number).AsList();

            _comic.ImageCount = pageList.Count;

            if (pageSizes.Count != _comic.PageCount)
            {
                throw new ApplicationException($"pageSizes is {pageSizes.Count} should be {_comic.PageCount}");
            }

            return pageList;
        }
    }
}
