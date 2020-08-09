using CoreComicsConverter.Cmxlgy;
using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreComicsConverter.DirectoryFlow
{
    class DirectoryConversionFlow
    {
        public bool IsDownload(DirectoryComic comic)
        {
            var isDownload= comic.Files.All(f => CmxlgyTools.IsDownload(Path.GetFileName(f)));

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
                    var fixedSize = false;

                    var factor = (double)doublePageSize.height / batch.Height;
                    var newWidth = Convert.ToInt32(batch.Width * factor);
                    if (newWidth == doublePageSize.width)
                    {
                        fixedSize = true;
                    }
                    else
                    { 
                        factor = (double)doublePageSize.width / batch.Width;
                        var newHeight = Convert.ToInt32(batch.Height * factor);
                        if (newHeight == doublePageSize.height)
                        {
                            fixedSize = true;
                        }
                    }

                    if (fixedSize)
                    {
                        batch.NewWidth = doublePageSize.width;
                        batch.NewHeight = doublePageSize.height;

                        ProgressReporter.Warning($"Fixed {batch.PageNumbers.Count} doublepage spreads: {batch.Width} x {batch.Height} -> {batch.NewWidth} x {batch.NewHeight}");
                    }
                }
            }
        }
    }
}
