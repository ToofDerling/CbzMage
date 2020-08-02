using Rotvel.PdfConverter.Extensions;
using Rotvel.PdfConverter.Helpers;
using Rotvel.PdfConverterCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rotvel.PdfConverter
{
    public class PdfComicConverter
    {
        public CompressCbzTask ConvertToCbz(Pdf pdf, CompressCbzTask compressCbzTask)
        {
            pdf.CreateOutputDirectory();

            var sortedImageSizes = ParsePdfImages(pdf);
            var wantedImageWidth = ParseImageSizes(sortedImageSizes);

            var dpi = CalculateDpiForImageSize(pdf, wantedImageWidth);
            var pageLists = CreatePageLists(pdf);

            WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            var allReadPages = ReadPages(pdf, pageLists, dpi);

            WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            ConvertPages(pdf, allReadPages);

            WaitForCompressPages(compressCbzTask);

            return StartCompressPages(pdf);
        }

        public void WaitForCompressPages(CompressCbzTask compressCbzTask, bool onlyCheckIfCompleted = false)
        {
            if (compressCbzTask == null || compressCbzTask.Pdf.CbzFileCreated)
            {
                return;
            }

            if (!compressCbzTask.IsCompleted)
            {
                if (onlyCheckIfCompleted)
                {
                    return;
                }

                Console.WriteLine($"WAIT {compressCbzTask.Pdf.GetCbzName()}");
                compressCbzTask.Wait();
            }

            compressCbzTask.Pdf.CleanOutputDirectory();
            compressCbzTask.Pdf.CbzFileCreated = true;

            Console.WriteLine($"FINISH {compressCbzTask.Pdf.GetCbzName()}");
        }

        public CompressCbzTask StartCompressPages(Pdf pdf)
        {
            var compressCbzTask = new CompressCbzTask(pdf);

            Console.WriteLine($"START {compressCbzTask.Pdf.GetCbzName()}");
            compressCbzTask.Start();

            return compressCbzTask;
        }

        private List<(int width, int height, int count)> ParsePdfImages(Pdf pdf)
        {
            var progressReporter = new ProgressReporter(pdf.PageCount);

            var pdfImageParser = new PdfParser();
            pdfImageParser.PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page-{e.CurrentPage}");

            var imageSizesMap = pdfImageParser.ParseImages(pdf);

            Console.WriteLine();
            Console.WriteLine($"{pdf.ImageCount} images");

            var parserErrors = pdfImageParser.GetImageParserErrors();
            parserErrors.ForEach(ex => Console.WriteLine(ex.TypeAndMessage()));

            return imageSizesMap;
        }

        private int ParseImageSizes(List<(int width, int height, int count)> sortedImageSizes)
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

        private int CalculateDpiForImageSize(Pdf pdf, int wantedImageWidth)
        {
            Console.WriteLine($"Wanted width: {wantedImageWidth}");

            var dpiCalculator = new DpiCalculator(pdf, wantedImageWidth);
            dpiCalculator.DpiCalculated += (s, e) => Console.WriteLine($" {e.Dpi} ({e.MinimumDpi}) -> {e.Width}");

            var dpi = dpiCalculator.CalculateDpi();

            Console.WriteLine($"Selected dpi: {dpi}");
            return dpi;
        }

        private List<int>[] CreatePageLists(Pdf pdf)
        {
            var pageChunker = new PageChunker();
            var pageLists = pageChunker.CreatePageLists(pdf.PageCount, 2, Program.ParallelThreads);

            var sb = new StringBuilder();
            sb.Append(Program.ParallelThreads).Append(" page threads: ");

            Array.ForEach(pageLists, p => sb.Append(p.Count).Append(' '));
            Console.WriteLine(sb);

            return pageLists;
        }

        private ConcurrentQueue<(string name, int number)> ReadPages(Pdf pdf, List<int>[] pageLists, int dpi)
        {
            var allReadPages = new ConcurrentQueue<(string name, int number)>();

            // We have the first page already
            allReadPages.Enqueue((pdf.GetPngPageString(1), 1));

            var progressReporter = new ProgressReporter(pdf.PageCount);

            Parallel.For(0, Program.ParallelThreads, (index, state) =>
            {
                var pageList = pageLists[index];

                var machine = new GhostscriptPageMachine();

                machine.PageRead += (s, e) =>
                {
                    allReadPages.Enqueue((e.Name, e.Number));
                    progressReporter.ShowProgress($"Read {e.Name}");
                };

                machine.ReadPageList(pdf, pageList, index, dpi);
            });

            Console.WriteLine();

            if (allReadPages.Count != pdf.PageCount)
            {
                throw new SomethingWentWrongException($"{nameof(allReadPages)} is {allReadPages.Count} should be {pdf.PageCount}");
            }

            return allReadPages;
        }

        private void ConvertPages(Pdf pdf, ConcurrentQueue<(string name, int number)> allReadPages)
        {
            var progressReporter = new ProgressReporter(pdf.PageCount);

            var pageConverter = new PageConverter();

            Parallel.For(0, Program.ParallelThreads, (index, state) =>
            {
                while (allReadPages.TryDequeue(out var page))
                {
                    var pngPath = Path.Combine(pdf.OutputDirectory, page.name);

                    var jpg = pdf.GetJpgPageString(page.number);
                    var jpgPath = Path.Combine(pdf.OutputDirectory, jpg);

                    pageConverter.ConvertPage(pngPath, jpgPath);

                    progressReporter.ShowProgress($"Converted {jpg}");
                }
            });

            Console.WriteLine();

            if (allReadPages.Count > 0)
            {
                throw new SomethingWentWrongException($"{nameof(allReadPages)} is {allReadPages.Count} should be 0");
            }
        }
    }
}
