using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using PdfConverter.Exceptions;
using PdfConverter.Ghostscript;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class ConverterEngine
    {
        private readonly GhostscriptPageMachineManager _pageMachineManager;

        public ConverterEngine(GhostscriptPageMachineManager pageMachineManager)
        {
            _pageMachineManager = pageMachineManager;
        }

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
            var parallelThreads = Settings.NumberOfThreads;

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
            // If Mode is Faster Value is a ManagedMemoryStream instance
            // If Mode is Slower Value is a string with the imagepath
            var convertedPages = new ConcurrentDictionary<string, object>(pageLists.Length, pdf.PageCount);

            var progressReporter = new ProgressReporter(pdf.PageCount);

            var pageCompressor = new PageCompressor(pdf, convertedPages, resizeHeight);
            pageCompressor.PagesCompressed += (s, e) => OnPagesCompressed(e);

            Pollux pollux = null;
            if (Settings.PdfConverterMode == PdfConverterMode.Slower)
            {
                pollux = new Pollux(pdf, pageLists, convertedPages);
                pollux.PageSaved += (s, e) => pageCompressor.OnPageConverted(e);
            }

            Parallel.For(0, pageLists.Length, (id) =>
            {
                var pageList = pageLists[id];

                var pageMachine = _pageMachineManager.StartMachine();

                if (Settings.PdfConverterMode == PdfConverterMode.Faster)
                {
                    var pageQueue = new Queue<int>(pageList);
                 
                    var pageConverter = new PageConverter(pdf, pageQueue, convertedPages, resizeHeight);
                    pageConverter.PageConverted += (s, e) => pageCompressor.OnPageConverted(e);

                    pageMachine.ReadPageList(pdf, pageList, dpi, pageConverter);
                    
                    pageConverter.WaitForPagesConverted();
                }
                else
                {
                    pollux.CreateOutputIdDir(id.ToString());

                    pageMachine.SavePageList(pdf, pageList, dpi, id.ToString());
                }

                _pageMachineManager.StopMachine(pageMachine);
            });

            if (Settings.PdfConverterMode == PdfConverterMode.Slower)
            {
                pollux.WaitForPagesSaved();
            }
            pageCompressor.SignalAllPagesConverted();

            pageCompressor.WaitForPagesCompressed();
            if (Settings.PdfConverterMode == PdfConverterMode.Slower)
            {
                // Now we can safely remove the temp dir and reset current directory.
                pollux.Cleanup();
            }
            
            Console.WriteLine();
            return pagesCompressed;

            void OnPagesCompressed(PagesCompressedEventArgs e)
            {
                pagesCompressed += e.Pages.Count();
            }
        }
    }
}
