using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using PdfConverter.Exceptions;
using PdfConverter.Ghostscript;
using PdfConverter.ManagedBuffers;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class ConverterEngine
    {
        public void ConvertToCbz(Pdf pdf, PdfImageParser pdfParser)
        {
            var sortedImageSizes = ParsePdfImages(pdf, pdfParser);
            var wantedWidth = GetWantedWidth(pdf, sortedImageSizes);

            var (dpi, dpiHeight) = CalculateDpiForWantedWidth(pdf, wantedWidth);
            var adjustedHeight = GetAdjustedHeight(pdf, sortedImageSizes, dpiHeight);

            var pageLists = CreatePageLists(pdf);
            var fileCount = ConvertPages(pdf, pageLists, dpi, adjustedHeight);

            if (fileCount != pdf.PageCount)
            {
                throw new SomethingWentWrongSorryException($"{fileCount} files generated for {pdf.PageCount} pages");
            }
        }

        private List<(int width, int height, int count)> ParsePdfImages(Pdf pdf, PdfImageParser pdfImageParser)
        {
            var progressReporter = new ProgressReporter(pdf.PageCount);

            pdfImageParser.PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page-{e.CurrentPage}");

            var imageSizesMap = pdfImageParser.ParseImages();

            Console.WriteLine();
            Console.WriteLine($"{pdf.ImageCount} images");

            var parserErrors = pdfImageParser.GetImageParserErrors();
            parserErrors.ForEach(ex => Console.WriteLine(ex.TypeAndMessage()));

            return imageSizesMap;
        }

        private int GetWantedWidth(Pdf pdf, List<(int width, int height, int count)> sortedImageSizes)
        {
            var mostOfThisSize = sortedImageSizes.First();

            var padLen = mostOfThisSize.count.ToString().Length;
            var cutOff = pdf.PageCount / 20;

            foreach (var (width, height, count) in sortedImageSizes.TakeWhile(x => x.width > 0 && x.count > cutOff))
            {
                Console.WriteLine($"  {count.ToString().PadLeft(padLen, ' ')}: {width} x {height}");
            }

            return mostOfThisSize.width;
        }

        private int? GetAdjustedHeight(Pdf pdf, List<(int width, int height, int count)> sortedImageSizes, int dpiHeight)
        {
            // The height of the image with the largest page count
            var realHeight = sortedImageSizes.First().height;

            // Check if the calculated wanted height is (much) larger than the real height
            var factor = 1.25;
            var checkHeight = realHeight * factor;

            if (dpiHeight > checkHeight)
            {
                // Get images sorted by height but only if their count is above the cutoff
                var cutOff = pdf.PageCount / 20;
                var sortedByHeight = sortedImageSizes.Where(x => x.count > cutOff).OrderByDescending(x => x.height);

                // If there's not any images with a count above the cutoff calculate the
                // average height and use that instead.
                var firstSortedHeight = sortedByHeight.FirstOrDefault();
                var largestRealHeight = firstSortedHeight != default
                    ? firstSortedHeight.height
                    : (int)sortedImageSizes.Average(x => x.height);

                // Don't set the new height too low.
                var adjustedHeight = Math.Max(largestRealHeight, Settings.MinimumHeight);
                // And only use it if it's sufficiently different than the wanted height
                if (adjustedHeight < (dpiHeight * 0.75))
                {
                    // Hard cap at the maximum height setting
                    adjustedHeight = Math.Min(Settings.MaximumHeight, adjustedHeight);

                    Console.WriteLine($"Adjusted height {dpiHeight} -> {adjustedHeight}");
                    return adjustedHeight;
                }
            }

            // Hard cap at the maximum height setting
            if (dpiHeight > Settings.MaximumHeight)
            {
                Console.WriteLine($"Adjusted height {dpiHeight} -> {Settings.MaximumHeight}");
                return Settings.MaximumHeight;
            }

            return null;
        }

        private (int dpi, int wantedHeight) CalculateDpiForWantedWidth(Pdf pdf, int wantedImageWidth)
        {
            Console.WriteLine($"Wanted width: {wantedImageWidth}");

            var pageMachine = new GhostscriptPageMachine();

            int dpiHeight = 0;

            var dpiCalculator = new DpiCalculator(pageMachine, pdf, wantedImageWidth);
            dpiCalculator.DpiCalculated += (s, e) =>
            {
                dpiHeight = e.Height;
                Console.WriteLine($"  {e.Dpi} -> {e.Width} x {dpiHeight}");
            };

            var dpi = dpiCalculator.CalculateDpi();

            Console.WriteLine($"Selected dpi: {dpi}");
            return (dpi, dpiHeight);
        }

        private List<int>[] CreatePageLists(Pdf pdf)
        {
            var parallelThreads = Settings.GhostscriptReaderThreads;
            if (pdf.PageCount < parallelThreads)
            {
                parallelThreads = 1;
            }

            //for (; parallelThreads > 0; parallelThreads--)
            //{ 

            //}
            //parallelThreads = 1;


            var pageChunker = new PageChunker();
            var pageLists = pageChunker.CreatePageLists(pdf.PageCount, parallelThreads);

            Array.ForEach(pageLists, p => Console.WriteLine($"  Reader{p.First()}: {p.Count} pages"));

            return pageLists;
        }

        private int ConvertPages(Pdf pdf, List<int>[] pageLists, int dpi, int? resizeHeight)
        {
            var pagesCompressed = 0;

            var pageSum = pageLists.Sum(p => p.Count);
            if (pageSum != pdf.PageCount)
            {
                throw new ApplicationException($"{nameof(pageLists)} pageSum {pageSum} should be {pdf.PageCount}");
            }

            // Each page converter is given a range of pages that are continously read and saved as png images.
            // The page compressor job thread picks up converted images as they are saved (in page order)
            // and creates the cbz file.

            // Key is page name (page-001.jpg etc)
            var convertedPages = new ConcurrentDictionary<string, ManagedMemoryStream>(pageLists.Length, pdf.PageCount);

            var progressReporter = new ProgressReporter(pdf.PageCount);

            var pageCompressor = new PageCompressor(pdf, convertedPages);
            pageCompressor.PagesCompressed += (s, e) => OnPagesCompressed(e);

            // If foundErrors > 0 we got a non-zero exitcode from the pagemachine and the lines
            // in linesQueue should be treated as errors instead of warnings.
            var foundErrors = 0;
            var linesQueue = new ConcurrentQueue<List<string>>();

            Parallel.For(0, pageLists.Length, (id) =>
            {
                var pageList = pageLists[id];
                var pageQueue = new Queue<int>(pageList);

                var pageConverter = new PageConverter(pdf, pageQueue, convertedPages, resizeHeight);
                pageConverter.PageConverted += (s, e) => pageCompressor.OnPageConverted(e);

                var pageMachine = new GhostscriptPageMachine();
                var (exitCode, warningsOrErrors) = pageMachine.ReadPageList(pdf, pageList, dpi, pageConverter);

                if (exitCode != 0 && foundErrors == 0)
                {
                    foundErrors++;
                }
                if (warningsOrErrors.Count> 0)
                {
                    linesQueue.Enqueue(warningsOrErrors);
                }

                pageConverter.WaitForPagesConverted();
            });

            pageCompressor.SignalAllPagesConverted();
            pageCompressor.WaitForPagesCompressed();

            Console.WriteLine();

            if (!linesQueue.IsEmpty)
            {
                var linesIsWarnings = foundErrors == 0;
                DumpWarningsOrErrors(linesIsWarnings, linesQueue);
            }

            return pagesCompressed;

            void OnPagesCompressed(PagesCompressedEventArgs e)
            {
                pagesCompressed += e.Pages.Count();
            }
        }

        private static void DumpWarningsOrErrors(bool linesIsWarnings, ConcurrentQueue<List<string>> linesQueue)
        {
            var linesDict = new Dictionary<string, int>();

            foreach (var foundLines in linesQueue)
            {
                foreach (var foundLine in foundLines)
                {
                    linesDict[foundLine] = linesDict.TryGetValue(foundLine, out var count) ? count + 1 : 1;
                }
            }

            var lines = new List<string>();

            foreach (var lineAndCount in linesDict)
            {
                var line = lineAndCount.Key;
                if (lineAndCount.Value > 1)
                {
                    line = $"{line} (x{lineAndCount.Value})";
                }
                lines.Add(line);
            }

            if (linesIsWarnings)
            {
                ProgressReporter.DumpWarnings(lines);
            }
            else
            {
                ProgressReporter.DumpErrors(lines);
            }
        }
    }
}
