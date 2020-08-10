using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using ImageMagick;
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
        public List<Page> ParseImages(PdfComic pdfComic)
        {
            pdfComic.CreateOutputDirectory();

            List<Page> pageSizes;
            using (var parser = new PdfImageParser(pdfComic))
            {
                pageSizes = parser.ParseImages(pdfComic);

                parser.GetParserWarnings().ForEach(warning => ProgressReporter.Warning(warning));
            }

            return pageSizes;
        }

        public void FixLargePageSize(List<PageBatch> pageBatches)
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

        public List<PageBatch> CalculateDpi(PdfComic pdfComic, List<PageBatch> pageBatches, out ConcurrentQueue<Page> readyPages)
        {
            Console.WriteLine($"Calculating dpi for {pageBatches.Count} imagesizes");

            var readyPagesBag = new ConcurrentBag<Page>();

            var queue = new ConcurrentQueue<PageBatch>(pageBatches);

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

            readyPages = new ConcurrentQueue<Page>(readyPagesBag);

            Console.WriteLine($"Read pages: {readyPages.Count}");

            // Trim image sizes that have been read fully
            return pageBatches.Where(i => i.Pages.Count > 0).AsList();
        }

        private static bool CalculateDpiForBatch(PdfComic pdfComic, PageBatch batch, ConcurrentBag<Page> readyPagesBag)
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

        public List<PageBatch> CoalescePageBatches(List<PageBatch> imageSizesList)
        {
            var dpiLookup = imageSizesList.ToLookup(i => i.Dpi);

            var coalescedList = new List<PageBatch>();

            foreach (var imageSizesForDpi in dpiLookup)
            {
                // Dpi and Pages are the only properties needed at this stage
                var coalesced = new PageBatch { Dpi = imageSizesForDpi.Key, Pages = new List<Page>() };

                coalescedList.Add(coalesced);

                foreach (var imageSize in imageSizesForDpi)
                {
                    coalesced.Pages.AddRange(imageSize.Pages);
                }
            }

            return coalescedList;
        }

        public List<PageBatch>[] ChunkPageBatches(List<PageBatch> pageBatches)
        {
            var pagesCount = pageBatches.Sum(p => p.Pages.Count);

            var chunkListPageCount = pagesCount / Settings.ParallelThreads;

            if (pagesCount % Settings.ParallelThreads > 0)
            {
                chunkListPageCount++;
            }

            var chunkLists = new List<List<PageBatch>>();

            for (var i = 0; i < Settings.ParallelThreads; i++)
            {
                chunkLists.Add(new List<PageBatch>());
            }

            pageBatches = pageBatches.OrderByDescending(p => p.Dpi).AsList();
            var queue = new Queue<PageBatch>(pageBatches);

            while (queue.Count > 0)
            {
                var batch = queue.Dequeue();

                while (batch.Pages.Count > 0)
                {
                    var chunkList = SmallestChunkList(chunkLists);

                    var lastChunk = chunkList.LastOrDefault();

                    var take = lastChunk != null ? chunkListPageCount - lastChunk.Pages.Count : chunkListPageCount;

                    var pages = batch.Pages.Take(take);

                    chunkList.Add(new PageBatch { Dpi = batch.Dpi, Pages = pages.AsList() });

                    batch.Pages = batch.Pages.Skip(take).AsList();
                }
            }

            foreach (var chunkList in chunkLists)
            {
                SortAndView(chunkList);
            }

            return chunkLists.ToArray();

            static List<PageBatch> SmallestChunkList(List<List<PageBatch>> chunkLists)
            {
                return chunkLists.OrderBy(l => l.Sum(p => p.Pages.Count)).First();
            }

            static void SortAndView(List<PageBatch> chunkList)
            {
                foreach (var chunk in chunkList)
                {
                    chunk.Pages = chunk.Pages.OrderBy(p => p.Number).AsList();
                    Console.Write($"{chunk.Dpi}: {chunk.Pages.Count} ");
                }
                Console.WriteLine();
            }
        }

        public void ReadPages(PdfComic pdfComic, List<PageBatch>[] pageLists, ConcurrentQueue<Page> readyPages)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount - readyPages.Count);

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                var pageList = pageLists[index];

                var machine = new GhostscriptPageMachine();

                machine.PageRead += (s, e) =>
                {
                    var page = e.Page;
                    page.Path = Path.Combine(pdfComic.OutputDirectory, page.Name);

                    readyPages.Enqueue(page);
                    progressReporter.ShowProgress($"Read {page.Name}");
                };

                foreach (var pageBatch in pageList)
                {
                    machine.ReadPageList(pdfComic, pageBatch);
                    pageBatch.Pages.Clear();
                }
            });

            Console.WriteLine();
        }
    }
}
