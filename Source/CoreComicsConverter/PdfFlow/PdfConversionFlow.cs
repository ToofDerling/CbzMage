using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreComicsConverter.PdfFlow
{
    public class PdfConversionFlow
    {
        public List<ComicPage> ParseImagesSetPageCount(PdfComic pdfComic)
        {
            using var parser = new PdfImageParser(pdfComic);

            var pageSizes = parser.ParseImages(pdfComic);

            parser.GetParserWarnings().ForEach(warning => ProgressReporter.Warning(warning));

            return pageSizes;
        }

        public ComicPageBatch CalculateDpi(PdfComic pdfComic, ComicPageBatch[] pageBatches, List<ComicPage> readPages)
        {
            // The AsList is necessary or the Skip below will sometimes not work as expected
            // causing the loop to add all pages in sortedBatches to mostOfThisSize
            var sortedBatches = pageBatches.OrderByDescending(b => b.Pages.Count).AsList();
            var mostOfThisSize = sortedBatches.First();

            var wantedHeight = mostOfThisSize.Height;
            Console.WriteLine($"Target height: {wantedHeight} ({mostOfThisSize.Pages.Count})");

            var readPage = CalculateDpiForBatch(pdfComic, mostOfThisSize);
            Console.WriteLine($"Calculated dpi: {mostOfThisSize.Dpi}");

            // Ensure that page read during calculation is not read again.
            readPages.Add(readPage);
            mostOfThisSize.Pages.Remove(readPage);

            foreach (var batch in sortedBatches.Skip(1))
            {
                mostOfThisSize.Pages.AddRange(batch.Pages);
            }

            return mostOfThisSize;
        }

        private static ComicPage CalculateDpiForBatch(PdfComic pdfComic, ComicPageBatch batch)
        {
            var page = batch.Pages.First();

            var dpiCalculator = new DpiCalculator(pdfComic, batch.Height, page);

            var calculatedDpi = dpiCalculator.CalculateDpi();

            if (calculatedDpi > Settings.MinimumDpi)
            {
                batch.Dpi = calculatedDpi;
            }
            else if (calculatedDpi == Settings.MinimumDpi)
            {
                batch.Dpi = Settings.MinimumDpi;
            }
            else
            {
                throw new ApplicationException($"{nameof(calculatedDpi)} is {calculatedDpi}???");
            }

            return page;
        }

        public ComicPageBatch[] ChunkPageBatch(ComicPageBatch pageBatch)
        {
            var sortedPages = pageBatch.Pages.OrderBy(p => p.Number);

            var numberOfPages = pageBatch.Pages.Count;
            var numberOfChunks = Settings.ParallelThreads;

            var chunkLists = new List<ComicPageBatch>();
            for (var i = 0; i < Settings.ParallelThreads; i++)
            {
                var chunk = new ComicPageBatch { Dpi = pageBatch.Dpi, Height = pageBatch.Height, Pages = new List<ComicPage>() };
                chunkLists.Add(chunk);
            }

            var counter = 0;
            foreach (var page in sortedPages)
            {
                var index = counter++ % numberOfChunks;

                chunkLists[index].Pages.Add(page);
            }

            return chunkLists.ToArray();
        }

        public void ReadPages(PdfComic pdfComic, ComicPageBatch[] pageBatches, List<ComicPage> readPages)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount - readPages.Count);

            var readPagesBag = new ConcurrentBag<ComicPage>();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                var chunk = pageBatches[index];

                var machine = new GhostscriptMachine();

                var chunkReadPages = machine.ReadPages(pdfComic, chunk, progressReporter);
                chunkReadPages.ForEach(readPagesBag.Add);

                chunk.Pages.Clear();
            });

            Console.WriteLine();

            readPages.AddRange(readPagesBag);
        }

        public bool AnalyzeImageSizes(List<ComicPage> readPages, int dpi, int targetHeight)
        {
            var queue = new ConcurrentQueue<ComicPage>(readPages);

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (!queue.IsEmpty)
                {
                    if (queue.TryDequeue(out var page))
                    {
                        page.Ping();
                    }
                }
            });

            if (readPages.Any(p => p.Height == 0))
            {
                throw new ApplicationException("One or more pages has a height of 0");
            }

            var sortedHeightsLookup = readPages.ToLookup(p => p.Height).OrderByDescending(l => l.AsList().Count);

            var firstLookup = sortedHeightsLookup.First();
            var firstLookupCount = firstLookup.Count();

            var newHeight = 0;
            if (dpi == Settings.MinimumDpi)
            {
                if (targetHeight < Settings.StandardHeight && firstLookup.Key > Settings.StandardHeight)
                {
                    newHeight = Settings.StandardHeight;
                }
            }
            else if (targetHeight > Settings.MaximumHeight)
            {
                newHeight = Settings.MaximumHeight;
            }

            bool changesWereMade = false;

            List<ComicPage> pages = firstLookup.AsList();
            var oldHeight = firstLookup.Key;

            SetNewHeight();

            if (newHeight == 0)
            {
                newHeight = firstLookup.Key;
            }

            foreach (var lookup in sortedHeightsLookup.Skip(1))
            {
                pages = lookup.AsList();
                oldHeight = lookup.Key;

                SetNewHeight();
            }

            return changesWereMade;

            void SetNewHeight()
            {
                if (newHeight == 0 || oldHeight == newHeight)
                {
                    Console.WriteLine($" {oldHeight} ({pages.Count})");
                    return;
                }

                Console.WriteLine($" {oldHeight} ({pages.Count}) -> {newHeight}");

                foreach (var page in pages)
                {
                    page.NewHeight = newHeight;
                }

                changesWereMade = true;
            }
        }
    }
}
