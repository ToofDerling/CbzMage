using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using ImageMagick;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreComicsConverter.Images
{
    public class DirectoryImageParser : IPageParser
    {
        private ConcurrentQueue<Page> _pageQueue;

        private readonly DirectoryComic _comic;

        public DirectoryImageParser(DirectoryComic comic)
        {
            _comic = comic;

            var sortedFiles = comic.Files.OrderBy(path => path.ToString());

            _pageQueue = new ConcurrentQueue<Page>();
            var pageNumber = 1;

            foreach (var file in sortedFiles)
            {
                _pageQueue.Enqueue(new Page { Number = pageNumber, Path = file });
                pageNumber++;
            }

            _comic.PageCount = _pageQueue.Count;
        }

        public event EventHandler<PageEventArgs> PageParsed;

        public List<Page> ParsePages()
        {
            var pageSizes = new ConcurrentBag<Page>();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (!_pageQueue.IsEmpty)
                {
                    if (_pageQueue.TryDequeue(out var page))
                    {
                        using (var image = new MagickImage())
                        {
                            image.Ping(page.Path);

                            page.Width = image.Width;
                            page.Height = image.Height;

                            pageSizes.Add(page);

                            PageParsed?.Invoke(this, new PageEventArgs(page));
                        };
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
