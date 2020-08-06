using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using ImageMagick;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoreComicsConverter.Images
{
    public class DirectoryImageParser : ImageParser
    {
        private ConcurrentQueue<(int number, string path)> _pageQueue;

        public DirectoryImageParser(Comic comic) : base(comic)
        { 
        }

        public override event EventHandler<PageEventArgs> PageParsed;

        public override void OpenComicSetPageCount() 
        {
            var sortedFiles = Directory.EnumerateFiles(_comic.Path).OrderBy(path => path.ToString());

            _pageQueue = new ConcurrentQueue<(int, string)>();
            var pageNumber = 1;

            foreach (var file in sortedFiles)
            {
                _pageQueue.Enqueue((pageNumber, file));
                pageNumber++;
            }

            _comic.PageCount = _pageQueue.Count;
        }

        public override List<(int pageNumber, int width, int height)> ParsePagesSetImageCount()
        {
            var pageSizes = new ConcurrentBag<(int pageNumber, int width, int height)>();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (!_pageQueue.IsEmpty)
                {
                    if (_pageQueue.TryDequeue(out (int number, string path) page))
                    {
                        using (var image = new MagickImage())
                        {
                            image.Ping(page.path);
                            pageSizes.Add((page.number, image.Width, image.Height));

                            PageParsed?.Invoke(this, new PageEventArgs($"page {page.number}"));
                        };
                    }
                }
            });

            var pageList = pageSizes.OrderBy(i => i.pageNumber).AsList();

            _comic.ImageCount = pageList.Count;

            if (pageSizes.Count != _comic.PageCount)
            {
                throw new ApplicationException($"pageSizes is {pageSizes.Count} should be {_comic.PageCount}");
            }

            return pageList;
        }
    }
}
