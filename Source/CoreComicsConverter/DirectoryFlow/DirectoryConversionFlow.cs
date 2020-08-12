using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.DirectoryFlow
{
    public class DirectoryConversionFlow
    {
        public bool VerifyDownload(DirectoryComic directoryComic)
        {
            var pagesMissing = CmxlgyTools.GetPagesMissing(directoryComic.Files, out var isMangaDownload);

            var manga = isMangaDownload ? "manga " : string.Empty;
            ProgressReporter.Info($"This looks like a Cmxlgy {manga}download");

            if (pagesMissing.Count > 0)
            {
                var missing = string.Join(',', pagesMissing.OrderBy(p => p).Select(p => p.ToString()));

                ProgressReporter.Error($"Pages {missing} is missing");
                return false;
            }
            return true;
        }

        public List<ComicPage> ParseImagesSetPageCount(DirectoryComic directoryComic)
        {
            var pageParser = new DirectoryImageParser(directoryComic);

            var pageSizes = pageParser.ParseImages(directoryComic);

            directoryComic.PageCount = pageSizes.Count;

            return pageSizes;
        }

        public void FixDoublePageSpreads(List<ComicPageBatch> pageBatches)
        {
            if (pageBatches.Count == 1)
            {
                return;
            }

            var sorted = pageBatches.OrderByDescending(p => p.Pages.Count);

            var mostOfThisSize = sorted.First();

            (int width, int height) = (mostOfThisSize.Width * 2, mostOfThisSize.Height);

            foreach (var batch in sorted.Skip(1))
            {
                if (batch.Height < mostOfThisSize.Height)
                {
                    if (CheckSize(height, batch.Height, batch.Width, width) || CheckSize(width, batch.Width, batch.Height, height))
                    {
                        batch.NewWidth = width;
                        batch.NewHeight = height;

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

        public List<ComicPage> GetPagesToConvert(List<ComicPageBatch> batches)
        {
            var pagesToConvert = new List<ComicPage>();

            foreach (var batch in batches)
            {
                if (batch.NewWidth > 0 && batch.NewHeight > 0)
                {
                    foreach (var page in batch.Pages)
                    {
                        page.NewWidth = batch.NewWidth;
                        page.NewHeight = batch.NewHeight;
                    }
                }

                pagesToConvert.AddRange(batch.Pages);
                batch.Pages.Clear();
            }

            return pagesToConvert;
        }
    }
}
