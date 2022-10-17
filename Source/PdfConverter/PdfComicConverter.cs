using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.Ghostscript;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class PdfComicConverter
    {
        private readonly GhostscriptPageMachineManager _pageMachineManager;

        public PdfComicConverter(GhostscriptPageMachineManager pageMachineManager)
        {
            _pageMachineManager = pageMachineManager;
        }

        public void ConvertToCbz(Pdf pdf, PdfImageParser pdfParser)
        {
            var sortedImageSizes = ParsePdfImages(pdf, pdfParser);
            var wantedWidth = GetWantedWidth(pdf, sortedImageSizes);

            var (dpi, dpiHeight) = CalculateDpiForImageSize(pdf, wantedWidth);
            var adjustedHeight = GetAdjustedHeight(pdf, sortedImageSizes, dpiHeight);

            var pageRanges = CreatePageLists(pdf);
            var fileCount = ConvertPages(pdf, pageRanges, dpi, adjustedHeight);

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

        private int? GetAdjustedHeight(Pdf pdf,
            List<(int width, int height, int count)> sortedImageSizes, int dpiHeight)
        {
            // The height if the image with the largest (page) count
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
                var newWantedHeight = Math.Max(largestRealHeight, Settings.MinimumHeight);
                // And only use it if it's sufficiently different than the wanted height
                if (newWantedHeight < (dpiHeight * 0.75))
                {
                    // Hard cap at the maximum height setting
                    newWantedHeight = Math.Min(Settings.MaximumHeight, newWantedHeight);

                    Console.WriteLine($"Adjusting wanted height {dpiHeight} -> {newWantedHeight}");
                    return newWantedHeight;
                }
            }

            // Hard cap at the maximum height setting
            if (dpiHeight > Settings.MaximumHeight)
            {
                Console.WriteLine($"Adjusting wanted height {dpiHeight} -> {Settings.MaximumHeight}");
                return Settings.MaximumHeight;
            }

            return null;
        }

        private (int dpi, int wantedHeight) CalculateDpiForImageSize(Pdf pdf, int wantedImageWidth)
        {
            Console.WriteLine($"Wanted width: {wantedImageWidth}");

            var pageMachine = _pageMachineManager.StartMachine();

            int dpiHeight = 0;

            var dpiCalculator = new DpiCalculator(pageMachine, pdf, wantedImageWidth);
            dpiCalculator.DpiCalculated += (s, e) =>
            {
                dpiHeight = e.Height;
                Console.WriteLine($"  {e.Dpi} -> {e.Width} x {dpiHeight}");
            };

            var dpi = dpiCalculator.CalculateDpi();

            _pageMachineManager.StopMachine(pageMachine);

            Console.WriteLine($"Selected dpi: {dpi}");
            return (dpi, dpiHeight);
        }

        private List<int>[] CreatePageLists(Pdf pdf)
        {
            var parallelThreads = Settings.ThreadCount;

            var pageChunker = new PageChunker();
            var pageLists = pageChunker.CreatePageLists(pdf.PageCount, parallelThreads);

            Array.ForEach(pageLists, p => Console.WriteLine($"  Reader{p.First()}: {p.Count} pages"));

            return pageLists;
        }

        private int ConvertPages(Pdf pdf, List<int>[] pageLists, int dpi, int? adjustedHeight)
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

            var convertedPages = new ConcurrentDictionary<string, MagickImage>(pageLists.Length, pdf.PageCount);

            var progressReporter = new ProgressReporter(pdf.PageCount);

            var pageCompressor = new PageCompressor(pdf, convertedPages);
            pageCompressor.PagesCompressed += (s, e) => OnPagesCompressed(e);

            Parallel.ForEach(pageLists, (pageList) =>
            {
                var pageQueue = new Queue<int>(pageList);
                var pageConverter = new PageConverter(pdf, pageQueue, convertedPages, adjustedHeight);

                pageConverter.PageConverted += (s, e) => pageCompressor.OnPageConverted(e);

                var pageMachine = _pageMachineManager.StartMachine();
                pageMachine.ReadPageList(pdf, pageList, dpi, pageConverter);

                _pageMachineManager.StopMachine(pageMachine);

                pageConverter.WaitForPagesConverted();
            });

            pageCompressor.SignalAllPagesConverted();
            pageCompressor.WaitForPagesCompressed();

            Console.WriteLine();
            return pagesCompressed;

            void OnPagesCompressed(PagesCompressedEventArgs e)
            {
                pagesCompressed += e.Pages.Count();
            }
        }
    }
}
