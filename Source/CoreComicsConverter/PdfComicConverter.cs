using CoreComicsConverter.Extensions;
using CoreComicsConverter.Images;
using CoreComicsConverter.Model;
using Org.BouncyCastle.Crypto.Prng;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreComicsConverter
{
    public class PdfComicConverter
    {
        public CompressCbzTask ConvertToCbz(Comic comic, CompressCbzTask compressCbzTask)
        {
            Console.WriteLine(comic.Path);

            var pageSizes = ParseComic(comic);
            var imageSizesList = CreateImageSizesList(comic, pageSizes);

            switch (comic.Type)
            {
                case ComicType.Pdf:
                    ConversionFlow((PdfComic)comic, imageSizesList);
                    break;
                case ComicType.Directory:
                    ConversionFlow((DirectoryComic)comic, imageSizesList);
                    break;
                default:
                    throw new ApplicationException($"Invalid type {comic.Type}");
            }

            //var dpi = CalculateDpiForImageSize(comic, 300);
            //var pageLists = CreatePageLists(comic);

            //WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            //var allReadPages = ReadPages(comic, pageLists, dpi);

            //WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            //ConvertPages(comic, allReadPages);

            //WaitForCompressPages(compressCbzTask);

            return null;

            //return StartCompressPages(comic);
        }

        // Pdf conversion flow     
        private void ConversionFlow(PdfComic pdfComic, List<Pages> pageBatches)
        {
            pdfComic.CreateOutputDirectory();

            pageBatches = CalculateDpi(pdfComic, pageBatches, out var allReadPages);
            VerifyPageBatches(pdfComic, allReadPages, pageBatches);

            pageBatches = CoalescePageBatches(pageBatches);
            VerifyPageBatches(pdfComic, allReadPages, pageBatches);

            var chunkedPageBatches = ChunkPageBatches(pageBatches);
            VerifyPageBatches(pdfComic, allReadPages, chunkedPageBatches);
        }

        private void ConversionFlow(DirectoryComic pdfComic, List<Pages> imageSizesList)
        {
            //pdfComic.CreateOutputDirectory();
            //var allreadPages = CalculateDpi(pdfComic, imageSizesList);
            CoalescePageBatches(imageSizesList);
        }



        public void WaitForCompressPages(CompressCbzTask compressCbzTask, bool onlyCheckIfCompleted = false)
        {
            if (compressCbzTask == null || compressCbzTask.PdfComic.CbzFileCreated)
            {
                return;
            }

            if (!compressCbzTask.IsCompleted)
            {
                if (onlyCheckIfCompleted)
                {
                    return;
                }

                Console.WriteLine($"WAIT {compressCbzTask.PdfComic.GetCbzName()}");
                compressCbzTask.Wait();
            }

            compressCbzTask.PdfComic.CleanOutputDirectory();
            compressCbzTask.PdfComic.CbzFileCreated = true;

            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine($"FINISH {compressCbzTask.PdfComic.GetCbzName()}");
            Console.ForegroundColor = oldColor;
        }

        private static CompressCbzTask StartCompressPages(PdfComic pdfComic)
        {
            var compressCbzTask = new CompressCbzTask(pdfComic);

            Console.WriteLine($"START {compressCbzTask.PdfComic.GetCbzName()}");
            compressCbzTask.Start();

            return compressCbzTask;
        }

        private static List<(int pageNumber, int width, int height)> ParseComic(Comic comic)
        {
            using ImageParser parser = ImageParserFactory.CreateFrom(comic);

            parser.OpenComicSetPageCount();
            Console.WriteLine($"{comic.PageCount} pages");

            var progressReporter = new ProgressReporter(comic.PageCount);
            parser.PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing {e.Name}");

            var pageSizes = parser.ParsePagesSetImageCount();

            Console.WriteLine();
            Console.WriteLine($"{comic.ImageCount} images");

            var parserErrors = parser.GetParserWarnings();
            parserErrors.ForEach(warning => Console.WriteLine(warning));

            return pageSizes;
        }

        private static List<Pages> CreateImageSizesList(Comic comic, List<(int pageNumber, int width, int height)> pageSizes)
        {
            // Group the pages by imagesize and sort with largest size first
            var sizeLookup = pageSizes.ToLookup(i => (i.width, i.height)).OrderByDescending(i => i.Key.width * i.Key.height);

            var imageSizesList = new List<Pages>();

            // Flatten the lookup
            foreach (var size in sizeLookup)
            {
                var pageNumbers = size.Select(s => s.pageNumber).AsList();

                imageSizesList.Add(new Pages { Width = size.Key.width, Height = size.Key.height, FirstPageNumber = pageNumbers.First(), PageNumbers = pageNumbers });
            }

            var pagesCount = imageSizesList.Sum(i => i.PageNumbers.Count);
            if (pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"imageSizesList pagesCount is {pagesCount} should be {comic.PageCount}");
            }

            return imageSizesList;
        }

        private static List<Pages> CalculateDpi(PdfComic pdfComic, List<Pages> imageSizesList, out ConcurrentQueue<(string name, int number)> allReadPages)
        {
            Console.WriteLine($"Calculating dpi for {imageSizesList.Count} imagesizes)");

            var readPages = new ConcurrentQueue<(string name, int number)>();

            var imageSizesQueue = new ConcurrentQueue<Pages>(imageSizesList);

            var readUsingMinimumDpi = false;

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (!imageSizesQueue.IsEmpty)
                {
                    if (imageSizesQueue.TryDequeue(out var imageSize))
                    {
                        if (readUsingMinimumDpi || imageSize.Width == 0)
                        {
                            imageSize.Dpi = Settings.MinimumDpi;
                        }
                        else
                        {
                            readUsingMinimumDpi = CalculateDpiFromImageSize(pdfComic, imageSize, readPages);
                        }
                    }
                }
            });

            foreach (var imageSize in imageSizesList)
            {
                if (imageSize.Dpi < Settings.MinimumDpi)
                {
                    throw new ApplicationException($"{imageSize.Width} x {imageSize.Height} dpi is {imageSize.Dpi} should be {Settings.MinimumDpi} or higher");
                }
            }

            allReadPages = readPages;

            Console.WriteLine($"Read pages: {allReadPages.Count}");

            // Trim image sizes that have been read fully
            return imageSizesList.Where(i => i.PageNumbers.Count > 0).AsList();
        }

        public static bool CalculateDpiFromImageSize(PdfComic pdfComic, Pages imageSize, ConcurrentQueue<(string name, int number)> allReadPages)
        {
            var readUsingMinimumDpi = false;

            var dpiCalculator = new DpiCalculator(pdfComic, (imageSize.FirstPageNumber, imageSize.Width, imageSize.Height));

            var calculatedDpi = dpiCalculator.CalculateDpi();
            var (currentWidth, currentHeight) = dpiCalculator.GetCurrentImageSize();

            Console.WriteLine($"{imageSize.Width} x {imageSize.Height} -> {calculatedDpi} ({currentWidth} x {currentHeight})");

            // Ensure that page read during calculation won't be read again.
            allReadPages.Enqueue((dpiCalculator.GetCurrentPage(), imageSize.FirstPageNumber));

            imageSize.PageNumbers.Remove(imageSize.FirstPageNumber);

            if (calculatedDpi > Settings.MinimumDpi)
            {
                imageSize.Dpi = calculatedDpi;
            }
            else if (calculatedDpi == Settings.MinimumDpi)
            {
                imageSize.Dpi = Settings.MinimumDpi;

                // Set rest of page batches to minimum dpi
                readUsingMinimumDpi = true;
            }
            else
            {
                throw new ApplicationException($"{nameof(calculatedDpi)} is {calculatedDpi}???");
            }

            return readUsingMinimumDpi;
        }

        private static List<Pages> CoalescePageBatches(List<Pages> imageSizesList)
        {
            var dpiLookup = imageSizesList.ToLookup(i => i.Dpi);

            var coalescedList = new List<Pages>();

            foreach (var imageSizesForDpi in dpiLookup)
            {
                // Dpi and Pages are the only properties needed at this stage
                var coalesced = new Pages { Dpi = imageSizesForDpi.Key, PageNumbers = new List<int>() };

                coalescedList.Add(coalesced);

                foreach (var imageSize in imageSizesForDpi)
                {
                    coalesced.PageNumbers.AddRange(imageSize.PageNumbers);
                }
            }

            return coalescedList;
        }

        private static void VerifyPageBatches(Comic comic, ConcurrentQueue<(string name, int number)> allReadPages, params List<Pages>[] pageBatches)
        {
            var pagesCount = pageBatches.Sum(batch => batch.Sum(i => i.PageNumbers.Count));
            if (allReadPages.Count + pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(pageBatches)} pages is {pagesCount} should be {comic.PageCount - allReadPages.Count}");
            }
        }

        private static List<Pages>[] ChunkPageBatches(List<Pages> pageBatches)
        {
            var pagesCount = pageBatches.Sum(p => p.PageNumbers.Count);

            var chunkListPageCount = pagesCount / Settings.ParallelThreads;

            if (pagesCount % Settings.ParallelThreads > 0)
            {
                chunkListPageCount++;
            }

            var chunkLists = new List<List<Pages>>();

            for (var i = 0; i < Settings.ParallelThreads; i++)
            {
                chunkLists.Add(new List<Pages>());
            }

            pageBatches = pageBatches.OrderByDescending(p => p.Dpi).AsList();
            var queue = new Queue<Pages>(pageBatches);

            while (queue.Count > 0)
            {
                var batch = queue.Dequeue();

                while (batch.PageNumbers.Count > 0)
                {
                    var chunkList = SmallestChunkList(chunkLists);

                    var lastChunk = chunkList.LastOrDefault();

                    var take = lastChunk != null ? chunkListPageCount - lastChunk.PageNumbers.Count : chunkListPageCount;

                    var pageNumbers = batch.PageNumbers.Take(take);

                    chunkList.Add(new Pages { Dpi = batch.Dpi, PageNumbers = pageNumbers.AsList() });

                    batch.PageNumbers = batch.PageNumbers.Skip(take).AsList();
                }
            }

            foreach (var chunkList in chunkLists)
            {
                SortAndView(chunkList);
            }

            return chunkLists.ToArray();

            static List<Pages> SmallestChunkList(List<List<Pages>> chunkLists)
            {
                return chunkLists.OrderBy(l => l.Sum(p => p.PageNumbers.Count)).First();
            }

            static void SortAndView(List<Pages> chunkList)
            {
                foreach (var chunk in chunkList)
                {
                    chunk.PageNumbers.Sort();
                    Console.Write($"{chunk.Dpi}: {chunk.PageNumbers.Count} ");
                }
                Console.WriteLine();
            }
        }
        private static int ParseImageSizes(List<(int width, int height, int count)> sortedImageSizes)
        {
            var mostOfThisSize = sortedImageSizes.First();

            var largestSizes = sortedImageSizes.Where(x => x.width >= mostOfThisSize.width && x.width - mostOfThisSize.width <= 50).OrderByDescending(x => x.width);
            var largestSize = largestSizes.First();

            var largestSizesByCount = largestSizes.OrderByDescending(x => x.count);
            var largestSizeWithLargestCount = largestSizesByCount.First(x => x.width == largestSize.width);

            var padLen = mostOfThisSize.count.ToString().Length;

            foreach (var (width, height, count) in largestSizesByCount.TakeWhile(x => x.count >= largestSizeWithLargestCount.count))
            {
                Console.WriteLine($" {count.ToString().PadLeft(padLen, ' ')} {width} x {height}");
            }

            return largestSize.width;
        }

        private static List<int>[] CreatePageLists(PdfComic pdfComic)
        {
            var pageChunker = new PageChunker();
            var pageLists = pageChunker.CreatePageLists(pdfComic.PageCount, 2, Settings.ParallelThreads);

            var sb = new StringBuilder();
            sb.Append(Settings.ParallelThreads).Append(" page threads: ");

            Array.ForEach(pageLists, p => sb.Append(p.Count).Append(' '));
            Console.WriteLine(sb);

            return pageLists;
        }

        private static ConcurrentQueue<(string name, int number)> ReadPages(PdfComic pdfComic, List<int>[] pageLists, int dpi)
        {
            var allReadPages = new ConcurrentQueue<(string name, int number)>();

            // We have the first page already
            allReadPages.Enqueue((pdfComic.GetPngPageString(1), 1));

            var progressReporter = new ProgressReporter(pdfComic.PageCount);

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                var pageList = pageLists[index];

                var machine = new GhostscriptPageMachine();

                machine.PageRead += (s, e) =>
                {
                    allReadPages.Enqueue((e.Name, e.Number));
                    progressReporter.ShowProgress($"Read {e.Name}");
                };

                machine.ReadPageList(pdfComic, pageList, index, dpi);
            });

            Console.WriteLine();

            if (allReadPages.Count != pdfComic.PageCount)
            {
                throw new ApplicationException($"{nameof(allReadPages)} is {allReadPages.Count} should be {pdfComic.PageCount}");
            }

            return allReadPages;
        }

        private static void ConvertPages(PdfComic pdfComic, ConcurrentQueue<(string name, int number)> allReadPages)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount);

            var pageConverter = new PageConverter();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (allReadPages.TryDequeue(out var page))
                {
                    var pngPath = Path.Combine(pdfComic.OutputDirectory, page.name);

                    var jpg = pdfComic.GetJpgPageString(page.number);
                    var jpgPath = Path.Combine(pdfComic.OutputDirectory, jpg);

                    pageConverter.ConvertPage(pngPath, jpgPath);

                    progressReporter.ShowProgress($"Converted {jpg}");
                }
            });

            Console.WriteLine();

            if (allReadPages.Count > 0)
            {
                throw new ApplicationException($"{nameof(allReadPages)} is {allReadPages.Count} should be 0");
            }
        }
    }
}
