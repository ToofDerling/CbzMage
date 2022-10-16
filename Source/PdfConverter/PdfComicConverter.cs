using CbzMage.Shared.Extensions;
using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.Ghostscript;
using PdfConverter.Helpers;
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

        public void ConvertToCbz(Pdf pdf, PdfParser pdfParser)
        {
            var sortedImageSizes = ParsePdfImages(pdf, pdfParser);
            var wantedImageWidth = ParseImageSizes(sortedImageSizes);

            var dpi = CalculateDpiForImageSize(pdf, wantedImageWidth);

            var pageRanges = CreatePageLists(pdf);
            var fileCount = ConvertPages(pdf, pageRanges, dpi);

            if (fileCount != pdf.PageCount)
            {
                throw new SomethingWentWrongException($"{fileCount} files generated for {pdf.PageCount} pages");
            }
        }

        private List<(int width, int height, int count)> ParsePdfImages(Pdf pdf, PdfParser pdfImageParser)
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
                Console.WriteLine($" {count.ToString().PadLeft(padLen, ' ')} {width} x {height}");
            }

            return largestSize.width;
        }

        private int CalculateDpiForImageSize(Pdf pdf, int wantedImageWidth)
        {
            Console.WriteLine($"Wanted width: {wantedImageWidth}");

            var pageMachine = _pageMachineManager.StartMachine();

            var dpiCalculator = new DpiCalculator(pageMachine, pdf, wantedImageWidth);
            dpiCalculator.DpiCalculated += (s, e) => Console.WriteLine($" {e.Dpi} ({e.MinimumDpi}) -> {e.Width}");

            var dpi = dpiCalculator.CalculateDpi();

            _pageMachineManager.StopMachine(pageMachine);

            Console.WriteLine($"Selected dpi: {dpi}");
            return dpi;
        }

        private List<int>[] CreatePageLists(Pdf pdf)
        {
            // While testing new pipe reading code
            var parallelThreads = 1;// 3;// Math.Max(1, Environment.ProcessorCount / 2);

            var pageChunker = new PageChunker();
            var pageLists = pageChunker.CreatePageLists(pdf.PageCount, parallelThreads);

            Array.ForEach(pageLists, p => Console.WriteLine($" {p.First()} - {p.Count}"));

            return pageLists;
        }

        private int ConvertPages(Pdf pdf, List<int>[] pageLists, int dpi)
        {
            var pagesCompressed = 0;

            // Each page converter is given a range of pages that are continously read and converted to png. The converted data is written to a pipe (one per converter).
            // Each converter is assigned an image converter job thread. The thread reads from the pipe and converts the png data to jpg files and saves them to memory.
            // The page compressor job thread picks up converted images as they are saved (in page order) and creates the cbz file.

            var convertedPages = new ConcurrentDictionary<string, MagickImage>(pageLists.Length, pdf.PageCount);

            var progressReporter = new ProgressReporter(pdf.PageCount * 2); // Both converting and compressing is counted

            var pageCompressor = new PageCompressor(pdf, convertedPages);
            pageCompressor.PagesCompressed += (s, e) => OnPagesCompressed(e);

            Parallel.ForEach(pageLists, (pageList) =>
            {
                var pageQueue = new Queue<int>(pageList);
                var pageConverter = new PageConverter(pdf, pageQueue, convertedPages);

                //pageConverter.PageConverted += (s, e) => progressReporter.ShowProgress($"Converted {e.Page}");
                pageConverter.PageConverted += (s, e) => pageCompressor.OnPageConverted(e);

                var pageMachine = _pageMachineManager.StartMachine();
                pageMachine.ReadPageList(pdf, pageList, dpi, pageConverter);

                _pageMachineManager.StopMachine(pageMachine);

                //pageConverter.WaitForPagesConverted();
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
