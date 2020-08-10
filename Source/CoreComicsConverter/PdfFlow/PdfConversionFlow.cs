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
        public List<Page> Initialize(PdfComic pdfComic, ComicConverter comicConverter)
        {
            pdfComic.CreateOutputDirectory();

            List<Page> pageSizes;
            using (var parser = new PdfImageParser(pdfComic))
            {
                pageSizes = comicConverter.ParseComic(pdfComic, parser);

                parser.GetParserWarnings().ForEach(warning => ProgressReporter.Warning(warning));
            }

            return pageSizes;
        }

        public void FixLargePageSize(List<PageBatch> pageBatches)
        {
            // Cannot fix a single page comic :)
            if (pageBatches.Count == 1 && pageBatches[0].PageNumbers.Count == 1)
            {
                return;
            }

            // If there's a single page much larger than the rest assume it's an error and try to fix it. 
            var largest = pageBatches[0];

            if (largest.PageNumbers.Count == 1)
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

        public List<PageBatch> CalculateDpi(PdfComic pdfComic, List<PageBatch> pageBatches, out ConcurrentQueue<Page> allReadPages)
        {
            Console.WriteLine($"Calculating dpi for {pageBatches.Count} imagesizes");

            var readPages = new ConcurrentQueue<Page>();

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
                            readUsingMinimumDpi = CalculateDpiForBatch(pdfComic, imageSize, readPages);
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

            allReadPages = readPages;

            Console.WriteLine($"Read pages: {allReadPages.Count}");

            // Trim image sizes that have been read fully
            return pageBatches.Where(i => i.PageNumbers.Count > 0).AsList();
        }

        private static bool CalculateDpiForBatch(PdfComic pdfComic, PageBatch batch, ConcurrentQueue<Page> allReadPages)
        {
            var readUsingMinimumDpi = false;

            var pageNumber = batch.FirstPage;

            var dpiCalculator = new DpiCalculator(pdfComic, (pageNumber, batch.Width, batch.Height));

            var calculatedDpi = dpiCalculator.CalculateDpi();
            var (currentWidth, currentHeight) = dpiCalculator.GetCurrentImageSize();

            Console.WriteLine($"{batch.Width} x {batch.Height} -> {calculatedDpi} ({currentWidth} x {currentHeight})");

            var pageName = dpiCalculator.GetCurrentPage();

            // Ensure that page read during calculation won't be read again.
            allReadPages.Enqueue(new Page { Name = pageName, Number = pageNumber, Path = Path.Combine(pdfComic.OutputDirectory, pageName) });
            batch.PageNumbers.Remove(pageNumber);

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
                var coalesced = new PageBatch { Dpi = imageSizesForDpi.Key, PageNumbers = new List<int>() };

                coalescedList.Add(coalesced);

                foreach (var imageSize in imageSizesForDpi)
                {
                    coalesced.PageNumbers.AddRange(imageSize.PageNumbers);
                }
            }

            return coalescedList;
        }

        public List<PageBatch>[] ChunkPageBatches(List<PageBatch> pageBatches)
        {
            var pagesCount = pageBatches.Sum(p => p.PageNumbers.Count);

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

                while (batch.PageNumbers.Count > 0)
                {
                    var chunkList = SmallestChunkList(chunkLists);

                    var lastChunk = chunkList.LastOrDefault();

                    var take = lastChunk != null ? chunkListPageCount - lastChunk.PageNumbers.Count : chunkListPageCount;

                    var pageNumbers = batch.PageNumbers.Take(take);

                    chunkList.Add(new PageBatch { Dpi = batch.Dpi, PageNumbers = pageNumbers.AsList() });

                    batch.PageNumbers = batch.PageNumbers.Skip(take).AsList();
                }
            }

            foreach (var chunkList in chunkLists)
            {
                SortAndView(chunkList);
            }

            return chunkLists.ToArray();

            static List<PageBatch> SmallestChunkList(List<List<PageBatch>> chunkLists)
            {
                return chunkLists.OrderBy(l => l.Sum(p => p.PageNumbers.Count)).First();
            }

            static void SortAndView(List<PageBatch> chunkList)
            {
                foreach (var chunk in chunkList)
                {
                    chunk.PageNumbers.Sort();
                    Console.Write($"{chunk.Dpi}: {chunk.PageNumbers.Count} ");
                }
                Console.WriteLine();
            }
        }

        public void ReadPages(PdfComic pdfComic, List<PageBatch>[] pageLists, ConcurrentQueue<Page> allReadPages)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount - allReadPages.Count);

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                var pageList = pageLists[index];

                var machine = new GhostscriptPageMachine();

                machine.PageRead += (s, e) =>
                {
                    var page = e.Page;
                    page.Path = Path.Combine(pdfComic.OutputDirectory, page.Name);

                    allReadPages.Enqueue(page);
                    progressReporter.ShowProgress($"Read {page.Name}");
                };

                foreach (var pageBatch in pageList)
                {
                    machine.ReadPageList(pdfComic, pageBatch);
                    pageBatch.PageNumbers.Clear();
                }
            });

            Console.WriteLine();
        }

        public void ConvertPages(PdfComic pdfComic, ConcurrentQueue<Page> allReadPages)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount);

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (allReadPages.TryDequeue(out var page))
                {
                    var jpg = ConvertPage(page);

                    progressReporter.ShowProgress($"Converted {jpg}");
                }
            });

            Console.WriteLine();

            if (allReadPages.Count > 0)
            {
                throw new ApplicationException($"{nameof(allReadPages)} is {allReadPages.Count} should be 0");
            }
        }

        private static string ConvertPage(Page page)
        {
            using var image = new MagickImage(page.Path)
            {
                Format = MagickFormat.Jpg,
                Interlace = Interlace.Plane,
                Quality = Settings.JpegQuality
            };

            var jpgPath = Path.ChangeExtension(page.Path, ".jpg");

            image.Write(jpgPath);
            File.Delete(page.Path);

            return Path.GetFileName(jpgPath);
        }
    }
}
