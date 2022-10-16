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
            var wantedWidth = ParseImageSizes(sortedImageSizes);

            var (dpi, wantedHeight) = CalculateDpiForImageSize(pdf, wantedWidth);

            var pageRanges = CreatePageLists(pdf);
            var fileCount = ConvertPages(pdf, pageRanges, dpi, wantedHeight);

            if (fileCount != pdf.PageCount)
            {
                throw new SomethingWentWrongException($"{fileCount} files generated for {pdf.PageCount} pages");
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
                Console.WriteLine($" {count.ToString().PadLeft(padLen, ' ')} in {width} x {height}");
            }

            return largestSize.width;
        }

        private (int dpi, int wantedHeight) CalculateDpiForImageSize(Pdf pdf, int wantedImageWidth)
        {
            Console.WriteLine($"Wanted width: {wantedImageWidth}");

            var pageMachine = _pageMachineManager.StartMachine();

            int wantedHeight = 0;

            var dpiCalculator = new DpiCalculator(pageMachine, pdf, wantedImageWidth);
            dpiCalculator.DpiCalculated += (s, e) =>
            {
                wantedHeight = e.Height;
                Console.WriteLine($" {e.Dpi} ({e.MinimumDpi}) -> {e.Width} x {wantedHeight}");
            };

            var dpi = dpiCalculator.CalculateDpi();

            _pageMachineManager.StopMachine(pageMachine);

            Console.WriteLine($"Selected dpi: {dpi}");
            return (dpi, wantedHeight);
        }

        private List<int>[] CreatePageLists(Pdf pdf)
        {
            var parallelThreads = Program.Settings.ThreadCount;

            var pageChunker = new PageChunker();
            var pageLists = pageChunker.CreatePageLists(pdf.PageCount, parallelThreads);

            Array.ForEach(pageLists, p => Console.WriteLine($" Reader{p.First()}: {p.Count} pages"));

            return pageLists;
        }

        private int ConvertPages(Pdf pdf, List<int>[] pageLists, int dpi, int wantedHeight)
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
                var pageConverter = new PageConverter(pdf, pageQueue, convertedPages, wantedHeight);

                pageConverter.PageConverted += (s, e) => pageCompressor.OnPageConverted(e);

                var pageMachine = _pageMachineManager.StartMachine();
                pageMachine.ReadPageList(pdf, pageList, dpi, pageConverter);

                pageConverter.WaitForPagesConverted();

                _pageMachineManager.StopMachine(pageMachine);
            });

            pageCompressor.SignalAllPagesConverted();
            pageCompressor.WaitForPagesCompressed();

            Console.WriteLine();
            return pagesCompressed;

            void OnPagesCompressed(PagesCompressedEventArgs e)
            {
                foreach (var page in e.Pages)
                {
                    pagesCompressed++;
                    progressReporter.ShowProgress($"Compressed {page}");
                }
            }
        }
    }
}
