using CoreComicsConverter.Cmxlgy;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.DirectoryFlow
{
    public class DirectoryConversionFlow
    {
        public bool IsDownload(DirectoryComic comic)
        {
            return  CmxlgyTools.IsDownload(comic.Files);
        }

        public bool VerifyDownload(DirectoryComic comic)
        {
            var pagesMissing = CmxlgyTools.GetPagesMissing(comic.Files, out var isMangaDownload);

            var manga = isMangaDownload ? "manga " : string.Empty;
            ProgressReporter.Info($"This looks like a Cmxlgy {manga}download");

            if (pagesMissing.Any())
            {
                var missing = string.Join(',', pagesMissing.OrderBy(p => p).Select(p => p.ToString()));

                ProgressReporter.Error($"Pages {missing} is missing");
                return false;
            }

            return true;
        }

        public List<Page> ParseImages(DirectoryComic comic)
        {
            var pageParser = new DirectoryImageParser(comic);
            
            var pageSizes = pageParser.ParseImages(comic);
            return pageSizes;
        }

        public void FixDoublePageSpreads(List<PageBatch> pageBatches)
        {
            if (pageBatches.Count == 1)
            {
                return;
            }

            var sorted = pageBatches.OrderByDescending(p => p.Pages.Count);

            var mostOfThisSize = sorted.First();

            (int width, int height) doublePageSize = (mostOfThisSize.Width * 2, mostOfThisSize.Height);

            foreach (var batch in sorted.Skip(1))
            {
                if (batch.Height < mostOfThisSize.Height)
                {
                    if (CheckSize(doublePageSize.height, batch.Height, batch.Width, doublePageSize.width)
                        || CheckSize(doublePageSize.width, batch.Width, batch.Height, doublePageSize.height))
                    {
                        batch.NewWidth = doublePageSize.width;
                        batch.NewHeight = doublePageSize.height;

                        ProgressReporter.Warning($"Fixed {batch.Pages.Count} doublepage spreads: {batch.Width} x {batch.Height} -> {batch.NewWidth} x {batch.NewHeight}");
                    }
                }

                static bool CheckSize(int large, int small, int other, int against)
                {
                    var factor = (double)large / small;
                    var compare = Convert.ToInt32(other * factor);
                    return compare == against;
                }
            }
        }

        public ConcurrentQueue<Page> GetPagesToConvert(List<PageBatch> batches)
        {
            var list = new List<Page>();

            foreach (var batch in batches)
            {
                if (batch.NewWidth > 0 && batch.NewHeight > 0)
                {
                    foreach (var page in batch.Pages)
                    {
                        page.NewWidth = batch.NewWidth;
                        page.NewHeight = batch.NewHeight;
                    }

                    list.AddRange(batch.Pages);
                }
                else
                {
                    list.AddRange(batch.Pages.Where(p => p.Path.EndsWithIgnoreCase(".png")));
                }
            }

            return new ConcurrentQueue<Page>(list);
        }
    }
}
