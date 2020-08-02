using CoreComicsConverter.Extensions;
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
        public CompressCbzTask ConvertToCbz(PdfComic pdfComic, CompressCbzTask compressCbzTask)
        {
            pdfComic.CreateOutputDirectory();

            var sortedImageSizes = ParsePdfImages(pdfComic);
            var wantedImageWidth = ParseImageSizes(sortedImageSizes);

            var dpi = CalculateDpiForImageSize(pdfComic, wantedImageWidth);
            var pageLists = CreatePageLists(pdfComic);

            WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            var allReadPages = ReadPages(pdfComic, pageLists, dpi);

            WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            ConvertPages(pdfComic, allReadPages);

            WaitForCompressPages(compressCbzTask);

            return StartCompressPages(pdfComic);
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

        private static List<(int width, int height, int count)> ParsePdfImages(PdfComic pdfComic)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount);

            var pdfImageParser = new PdfParser();
            pdfImageParser.PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page-{e.CurrentPage}");

            var imageSizesMap = pdfImageParser.ParseImages(pdfComic);

            Console.WriteLine();
            Console.WriteLine($"{pdfComic.ImageCount} images");

            var parserErrors = pdfImageParser.GetImageParserErrors();
            parserErrors.ForEach(ex => Console.WriteLine(ex.TypeAndMessage()));

            return imageSizesMap;
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
            Console.WriteLine($"Wanted width: {wantedImageWidth}");

            var dpiCalculator = new DpiCalculator(pdfComic, wantedImageWidth);
            dpiCalculator.DpiCalculated += (s, e) => Console.WriteLine($" {e.Dpi} ({e.MinimumDpi}) -> {e.Width}");

            var dpi = dpiCalculator.CalculateDpi();

            Console.WriteLine($"Selected dpi: {dpi}");
            return dpi;
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
