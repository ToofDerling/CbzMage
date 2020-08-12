using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoreComicsConverter.PdfFlow
{
    public class PdfConversionFlow
    {
        public List<ComicPage> ParseImagesSetPageCount(PdfComic pdfComic)
        {
            List<ComicPage> pageSizes;

            using var parser = new PdfImageParser(pdfComic);

            pageSizes = parser.ParseImages(pdfComic);

            parser.GetParserWarnings().ForEach(warning => ProgressReporter.Warning(warning));

            return pageSizes;
        }

        public void FixLargePageSize(List<ComicPageBatch> pageBatches)
        {
            // Cannot fix a single page comic :)
            if (pageBatches.Count == 1 && pageBatches[0].Pages.Count == 1)
            {
                return;
            }

            // If there's a single page much larger than the rest assume it's an error and try to fix it. 
            var largest = pageBatches[0];

            if (largest.Pages.Count == 1)
            {
                var secondLargest = pageBatches[1];

                if (largest.Width > secondLargest.Width * 2 && largest.Height > secondLargest.Height * 2)
                {
                    var oldHeight = largest.Height;
                    largest.Height = secondLargest.Height;

                    var factor = (double)oldHeight / largest.Height;

                    var oldWidth = largest.Width;
                    var newWidth = oldWidth / factor;

                    largest.Width = Convert.ToInt32(newWidth);

                    ProgressReporter.Warning($"Fixed page {largest.FirstPage}: {oldWidth} x {oldHeight} -> {largest.Width} x {largest.Height}");
                }
            }
        }

        public List<ComicPage> CalculateDpi(PdfComic pdfComic, List<ComicPageBatch> pageBatches)
        {
            Console.WriteLine($"Calculating dpi for {pageBatches.Count} imagesizes");

            var readyPagesBag = new ConcurrentBag<ComicPage>();

            var queue = new ConcurrentQueue<ComicPageBatch>(pageBatches);

            var readUsingMinimumDpi = false;

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (!queue.IsEmpty)
                {
                    if (queue.TryDequeue(out var imageSize))
                    {
                        if (readUsingMinimumDpi || imageSize.Width == 0)
                        {
                            imageSize.Dpi = Settings.MinimumDpi;
                        }
                        else
                        {
                            readUsingMinimumDpi = CalculateDpiForBatch(pdfComic, imageSize, readyPagesBag);
                        }
                    }
                }
            });

            foreach (var imageSize in pageBatches)
            {
                if (imageSize.Dpi < Settings.MinimumDpi)
                {
                    throw new ApplicationException($"{imageSize.Width} x {imageSize.Height} dpi is {imageSize.Dpi} should be {Settings.MinimumDpi} or higher");
                }
            }

            var pagesRead = new List<ComicPage>(readyPagesBag);
            Console.WriteLine($"Read pages: {pagesRead.Count}");

            // Trim image sizes that have been read fully
            pageBatches.RemoveAll(p => p.Pages.Count == 0);

            return pagesRead;
        }

        private static bool CalculateDpiForBatch(PdfComic pdfComic, ComicPageBatch batch, ConcurrentBag<ComicPage> readyPagesBag)
        {
            var readUsingMinimumDpi = false;

            var page = batch.Pages.First();

            var dpiCalculator = new DpiCalculator(pdfComic, batch, page);

            var calculatedDpi = dpiCalculator.CalculateDpi();

            Console.WriteLine($"{batch.Width} x {batch.Height} -> {calculatedDpi} ({page.Width} x {page.Height})");

            // Ensure that page read during calculation won't be read again.
            readyPagesBag.Add(page);
            batch.Pages.Remove(page);

            if (calculatedDpi > Settings.MinimumDpi)
            {
                batch.Dpi = calculatedDpi;
            }
            else if (calculatedDpi == Settings.MinimumDpi)
            {
                batch.Dpi = Settings.MinimumDpi;

                // Set rest of page batches to minimum dpi
                readUsingMinimumDpi = true;
            }
            else
            {
                throw new ApplicationException($"{nameof(calculatedDpi)} is {calculatedDpi}???");
            }

            return readUsingMinimumDpi;
        }

        public List<ComicPageBatch> CoalescePageBatches(List<ComicPageBatch> imageSizesList)
        {
            var dpiLookup = imageSizesList.ToLookup(i => i.Dpi);

            var coalescedList = new List<ComicPageBatch>();

            foreach (var imageSizesForDpi in dpiLookup)
            {
                // Dpi and Pages are the only properties needed at this stage
                var coalesced = new ComicPageBatch { Dpi = imageSizesForDpi.Key, Pages = new List<ComicPage>() };

                coalescedList.Add(coalesced);

                foreach (var imageSize in imageSizesForDpi)
                {
                    coalesced.Pages.AddRange(imageSize.Pages);
                }
            }

            return coalescedList;
        }

        public List<ComicPageBatch>[] ChunkPageBatches(List<ComicPageBatch> pageBatches)
        {
            var pagesCount = pageBatches.Sum(p => p.Pages.Count);

            var chunkListPageCount = pagesCount / Settings.ParallelThreads;

            if (pagesCount % Settings.ParallelThreads > 0)
            {
                chunkListPageCount++;
            }

            var chunkLists = new List<List<ComicPageBatch>>();

            for (var i = 0; i < Settings.ParallelThreads; i++)
            {
                chunkLists.Add(new List<ComicPageBatch>());
            }

            pageBatches = pageBatches.OrderByDescending(p => p.Dpi).AsList();
            var queue = new Queue<ComicPageBatch>(pageBatches);

            while (queue.Count > 0)
            {
                var batch = queue.Dequeue();

                while (batch.Pages.Count > 0)
                {
                    var chunkList = SmallestChunkList(chunkLists);

                    var lastChunk = chunkList.LastOrDefault();

                    var take = lastChunk != null ? chunkListPageCount - lastChunk.Pages.Count : chunkListPageCount;

                    var pages = batch.Pages.Take(take);

                    chunkList.Add(new ComicPageBatch { Dpi = batch.Dpi, Pages = pages.AsList() });

                    batch.Pages = batch.Pages.Skip(take).AsList();
                }
            }

            foreach (var chunkList in chunkLists)
            {
                SortAndView(chunkList);
            }

            return chunkLists.ToArray();

            static List<ComicPageBatch> SmallestChunkList(List<List<ComicPageBatch>> chunkLists) => chunkLists.OrderBy(l => l.Sum(p => p.Pages.Count)).First();

            static void SortAndView(List<ComicPageBatch> chunkList)
            {
                foreach (var chunk in chunkList)
                {
                    chunk.Pages = chunk.Pages.OrderBy(p => p.Number).AsList();

                    Console.Write($"{chunk.Dpi}: {chunk.Pages.Count} ");
                }
                Console.WriteLine();
            }
        }

        public void ReadPages(PdfComic pdfComic, List<ComicPageBatch>[] pageLists, List<ComicPage> readPages)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount - readPages.Count);

            var readPagesBag = new ConcurrentBag<ComicPage>();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                var pageList = pageLists[index];

                var machine = new GhostscriptMachine();

                machine.PageRead += (s, e) =>
                {
                    var page = e.Page;
                    page.Path = Path.Combine(pdfComic.OutputDirectory, page.Name);

                    readPagesBag.Add(page);
                    progressReporter.ShowProgress($"Read {page.Name}");
                };

                foreach (var pageBatch in pageList)
                {
                    machine.ReadPageList(pdfComic, pageBatch);
                    pageBatch.Pages.Clear();
                }
            });

            Console.WriteLine();

            readPages.AddRange(readPagesBag);
        }
    }
}
