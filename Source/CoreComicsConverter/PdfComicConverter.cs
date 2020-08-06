using CoreComicsConverter.Extensions;
using CoreComicsConverter.Images;
using CoreComicsConverter.Model;
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
            //comic.CreateOutputDirectory();

            var pageSizes = ParseComic(comic);
            var imageSizesList = CreateImageSizesList(comic, pageSizes);

            // Pdf conversion flow            
            var allreadPages = CalculateDpi((PdfComic)comic, imageSizesList);
            imageSizesList = CoalesceImageSizesList(comic, imageSizesList);

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

        private static List<ImageSize> CreateImageSizesList(Comic comic, List<(int pageNumber, int width, int height)> pageSizes)
        {
            // Group the pages by imagesize and sort with largest size first
            var sizeLookup = pageSizes.ToLookup(i => (i.width, i.height)).OrderByDescending(i => i.Key.width * i.Key.height);

            var imageSizesList = new List<ImageSize>();

            // Flatten the lookup
            foreach (var size in sizeLookup)
            {
                var pages = size.Select(s => s.pageNumber).AsList();

                imageSizesList.Add(new ImageSize { Width = size.Key.width, Height = size.Key.height, PageNumber = pages.First(), Pages = pages });
            }

            var pagesCount = imageSizesList.Sum(i => i.Pages.Count);
            if (pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"imageSizesList pagesCount is {pagesCount} should be {comic.PageCount}");
            }

            return imageSizesList;
        }


        private static ConcurrentQueue<(string name, int number)> CalculateDpi(PdfComic pdfComic, List<ImageSize> imageSizesList)
        {
            Console.WriteLine($"Calculating dpi for {imageSizesList.Count} imagesizes (minimum dpi is {Settings.MinimumDpi})");

            // Collect the pages read during calculation to avoid reading them again
            var allReadPages = new ConcurrentQueue<(string name, int number)>();

            var readUsingMinimumDpi = false;

            var imageSizesQueue = new ConcurrentQueue<ImageSize>(imageSizesList);

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (!imageSizesQueue.IsEmpty)
                {
                    if (imageSizesQueue.TryDequeue(out var size))
                    {
                        if (!readUsingMinimumDpi)
                        {
                            // Calculate dpi for this batch of pages
                            var dpiCalculator = new DpiCalculator(pdfComic, (size.PageNumber, size.Width, size.Height));

                            var calclulatedDpi = dpiCalculator.CalculateDpi();
                            var (currentWidth, currentHeight) = dpiCalculator.GetCurrentImageSize();

                            Console.WriteLine($"{size.Width} x {size.Height} -> {calclulatedDpi} ({currentWidth} x {currentHeight})");

                            if (calclulatedDpi > Settings.MinimumDpi)
                            {
                                size.Dpi = calclulatedDpi;
                                allReadPages.Enqueue((dpiCalculator.GetCurrentPage(), size.PageNumber));
                            }
                            else
                            {   // Set rest of page batches to minimum dpi
                                size.Dpi = Settings.MinimumDpi;
                                readUsingMinimumDpi = true;
                            }
                        }
                        else
                        {
                            size.Dpi = Settings.MinimumDpi;
                        }
                    }
                }
            });

            foreach (var imageSize in imageSizesList)
            {
                if (imageSize.Dpi < Settings.MinimumDpi)
                {
                    throw new ApplicationException($"dpi for {imageSize.Width} x {imageSize.Height} is {imageSize.Dpi} should be {Settings.MinimumDpi} or higher");
                }
            }

            return allReadPages;
        }

        private static List<ImageSize> CoalesceImageSizesList(Comic comic, List<ImageSize> imageSizesList)
        {
            var dpiLookup = imageSizesList.ToLookup(i => i.Dpi);
            
            var coalescedList = new List<ImageSize>();

            foreach (var imageSizesForDpi in dpiLookup)
            {
                // Dpi and Pages are the only properties needed at this stage
                var coalesced = new ImageSize { Dpi = imageSizesForDpi.Key, Pages = new List<int>() };

                coalescedList.Add(coalesced);

                foreach (var imageSize in imageSizesForDpi)
                {
                    coalesced.Pages.AddRange(imageSize.Pages);
                }
            }

            coalescedList = coalescedList.OrderByDescending(i => i.Dpi).AsList();

            var coalescedPagesCount = coalescedList.Sum(i => i.Pages.Count);

            if (coalescedPagesCount != comic.PageCount)
            {
                throw new ApplicationException($"coalescedList pagesCount is {coalescedPagesCount} should be {comic.PageCount}");
            }

            foreach (var imageSize in coalescedList)
            {
                imageSize.Pages.Sort();
                Console.WriteLine($"{imageSize.Dpi} {imageSize.Pages.Count} pages");
            }

            return coalescedList;
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

        private static int CalculateDpiForImageSize(PdfComic pdfComic, int wantedImageWidth)
        {
            //Console.WriteLine($"Wanted width: {wantedImageWidth}");

            //var dpiCalculator = new DpiCalculator(pdfComic, wantedImageWidth);
            //dpiCalculator.DpiCalculated += (s, e) => Console.WriteLine($" {e.Dpi} ({e.MinimumDpi}) -> {e.Width}");

            //var dpi = dpiCalculator.CalculateDpi();

            //Console.WriteLine($"Selected dpi: {dpi}");
            return 0;
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
