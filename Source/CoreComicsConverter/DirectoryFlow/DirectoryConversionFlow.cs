using CoreComicsConverter.Cmxlgy;
using CoreComicsConverter.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreComicsConverter.DirectoryFlow
{
    class DirectoryConversionFlow
    {
        public bool IsDownload(DirectoryComic comic)
        {
            var isDownload = CmxlgyTools.IsDownload(comic.Files);

            if (isDownload)
            {
                ProgressReporter.Info("This is assumed to be a Cmxlgy download");
            }

            return isDownload;
        }

        public bool VerifyDownload(DirectoryComic comic)
        {
            var pageMissing = CmxlgyTools.VerifyDownloadFiles(comic.Files, out var _);
            if (pageMissing > -1)
            {
                ProgressReporter.Error($"Page {pageMissing} is missing");
                return false;

            }
            return true;
        }

        public void FixDoublePageSpreads(List<PageBatch> pageBatches)
        {
            if (pageBatches.Count == 1)
            {
                return;
            }

            var sorted = pageBatches.OrderByDescending(p => p.PageNumbers.Count);

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

                        ProgressReporter.Warning($"Fixed {batch.PageNumbers.Count} doublepage spreads: {batch.Width} x {batch.Height} -> {batch.NewWidth} x {batch.NewHeight}");
                    }
                }

                bool CheckSize(int large, int small, int other, int against)
                {
                    var factor = (double)large / small;
                    var compare = Convert.ToInt32(other * factor);
                    return compare == against;
                }
            }
        }


        public ConcurrentQueue<Page> GetPagesToConvert(List<PageBatch> batches)
        {
            var queue = new ConcurrentQueue<Page>();

            foreach (var batch in batches)
            { 
                
            }

            return queue;
        }
    }
}
