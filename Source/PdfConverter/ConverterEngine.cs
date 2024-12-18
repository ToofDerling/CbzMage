using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using PdfConverter.Exceptions;
using PdfConverter.ImageConversion;
using PdfConverter.PageInfo;
using PdfConverter.PageMachines;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PdfConverter
{
    public class ConverterEngine
    {
        public async Task ConvertToCbzAsync(Pdf pdf, PdfParser pdfParser)
        {
            SetPageParsed(pdfParser, pdf);
            var sortedImageSizes = ParsePdfImages(pdf, pdfParser);

            // if renderPageCount < pageCount, ie we're saving some original images, pageToSaveMostOfThisSize is not null
            var (pageInfoMap, pageToSaveMostOfThisSize) = GetPageInfoMap(pdfParser, sortedImageSizes);
            var renderPageCount = pageInfoMap.Count(p => p.Value is PdfPageInfoRenderImage);

            int? adjustedHeight = null;
            int renderDpi = 0;

            if (renderPageCount == 0)
            {
                Console.WriteLine("Using original images on all pages");
                pdf.SaveDirectory.DeleteAndCreateDir();

                adjustedHeight = GetAdjustedHeightForOriginalImages(pageToSaveMostOfThisSize!);
            }
            else if (renderPageCount == pdf.PageCount)
            {
                Console.WriteLine("Original images not available");
                var pageWithWantedWidth = GetWantedWidth(pdf, sortedImageSizes, pageInfoMap);

                (renderDpi, int dpiHeight) = CalculateDpiForWantedWidth(pdf, pageWithWantedWidth);
                adjustedHeight = GetAdjustedHeightForRendering(pdf, sortedImageSizes, dpiHeight);
            }
            else
            {
                Console.WriteLine($"Using original images on {pdf.PageCount - renderPageCount} pages");
                pdf.SaveDirectory.DeleteAndCreateDir();

                (renderDpi, _) = CalculateDpiForWantedWidth(pdf, pageToSaveMostOfThisSize!);
                adjustedHeight = GetAdjustedHeightForOriginalImages(pageToSaveMostOfThisSize!);
            }

            if (adjustedHeight.HasValue)
            {
                Console.WriteLine($"Adjusted height: {adjustedHeight.Value}");
            }

            var imageProducers = new List<AbstractImageConverter>(pageInfoMap.Count);

            foreach (var page in pageInfoMap.OrderBy(p => p.Key))
            {
                if (adjustedHeight.HasValue)
                {
                    page.Value.ResizeHeight = adjustedHeight.Value;
                }

                if (page.Value is PdfPageInfoRenderImage renderImage)
                {
                    renderImage.Dpi = renderDpi;

                    imageProducers.Add(new PopplerRenderImageConverter(pdf, renderImage));
                }
                else if (page.Value is PdfPageInfoSaveImage saveImage)
                {
                    imageProducers.Add(new PopplerSaveImageConverter(pdf, saveImage));
                }
            }

            var fileCount = await ConvertPagesAsync(pdf, imageProducers);
            pdf.SaveDirectory.DeleteIfExists();

            if (!Settings.SaveCoverOnly && (fileCount != pdf.PageCount))
            {
                throw new SomethingWentWrongSorryException($"{fileCount} files generated for {pdf.PageCount} pages");
            }
        }

        private static void SetPageParsed(PdfParser pdfParser, Pdf pdf)
        {
            pdfParser.PageParsed += (s, e) =>
            {
                var parserMode = e.ParserMode switch
                {
                    ParserMode.Images => "images",
                    ParserMode.Text => "text",
                    _ => "?"
                };
                ProgressReporter.ShowProgress($"Parsing {parserMode} on page {e.CurrentPage}", e.CurrentPage, pdf.PageCount);
            };
        }

        private static List<(int width, int height, int count)> ParsePdfImages(Pdf pdf, PdfParser pdfParser)
        {
            var sortedImageSizes = pdfParser.AnalyzeImages();

            Console.WriteLine($"{pdf.ImageCount} images");

            var parserErrors = pdfParser.GetImageParserErrors();
            parserErrors.ForEach(x => Console.WriteLine($"{x.pageNumber.ToPageString()} - {x.exception.TypeAndMessage()}"));

            return sortedImageSizes;
        }

        private static (Dictionary<int, AbstractPdfPageInfo> pageInfoMap, AbstractPdfPageInfo? pageToSaveMostOfThisSize) GetPageInfoMap(PdfParser pdfParser,
            List<(int width, int height, int count)> sortedImageSizes)
        {
            var pageMap = pdfParser.GetPageMap();
            Debug.Assert(pageMap.Values.All(p => p is PdfPageInfoRenderImage));

            // Saving original images from pdf requires pages with exactly one image
            var pagesToSave = pageMap.Values.Where(p => p.ImageCount == 1 && p.LargestImage.height >= Settings.MinimumHeight && !IsCroppedDoublePageSpread(p)).ToList();

            // Detect cropping of double page spread original image
            static bool IsCroppedDoublePageSpread(AbstractPdfPageInfo pageInfo)
            {
                // If image is __ but page is | 
                return (pageInfo.LargestImage.width > pageInfo.LargestImage.height) && (pageInfo.PageSize.height > pageInfo.PageSize.width);
            }

            if (pagesToSave.Count == 0)
            {
                return (pageMap, null);
            }

            // Disable saving original images if all pages has any text to render
            pagesToSave = pdfParser.FilterPagesWithText(pagesToSave);

            var parserErrors = pdfParser.GetTextParserErrors();
            parserErrors.ForEach(x => Console.WriteLine($"{x.pageNumber.ToPageString()} - {x.exception.TypeAndMessage()}"));

            if (pagesToSave.Count == 0)
            {
                return (pageMap, null);
            }

            AbstractPdfPageInfo? pageToSaveMostOfThisSize = null;

            // Finally look at the height, unless all images are the same size
            if (sortedImageSizes.Count > 1)
            {
                foreach (var (width, height, _) in sortedImageSizes)
                {
                    if ((pageToSaveMostOfThisSize = pagesToSave.FirstOrDefault(p => p.LargestImage.width == width && p.LargestImage.height == height)) != null)
                    {
                        break;
                    }
                }
                Debug.Assert(pageToSaveMostOfThisSize != null);

                var highest = pageToSaveMostOfThisSize.LargestImage.height * 1.1d;
                var lowest = pageToSaveMostOfThisSize.LargestImage.height * 0.9d;

                // Ensure saved pages are roughly the same height 
                //return resizeHeight.HasValue && (resizeHeight < (height * 1.1d)) || (resizeHeight > (height * 0.9d));

                //pagesToSave = pagesToSave.Where(p => p.LargestImage.height <= highest && p.LargestImage.height >= lowest).ToList();


                pagesToSave = pagesToSave.Where(p => AbstractPdfPageInfo.UseResizeHeight(p.LargestImage.height, pageToSaveMostOfThisSize.LargestImage.height)).ToList();

                if (pagesToSave.Count == 0)
                {
                    return (pageMap, null);
                }
            }

            // Ensure pages to save gets the correct converter
            foreach (var page in pagesToSave)
            {
                pageMap[page.PageNumber]= (PdfPageInfoSaveImage)page;

                //pageMap[page.PageNumber] = new PdfPageInfoSaveImage(page.PageNumber)
                //{
                //    ImageCount = page.ImageCount,
                //    LargestImage = page.LargestImage,
                //    LargestImageExt = page.LargestImageExt,
                //    PageSize = page.PageSize,
                //};
            }

            return (pageMap, pageToSaveMostOfThisSize);
        }

        private static AbstractPdfPageInfo GetWantedWidth(Pdf pdf, List<(int width, int height, int count)> sortedImageSizes, Dictionary<int, AbstractPdfPageInfo> pageMap)
        {
            var mostOfThisSize = sortedImageSizes.First();

            var padLen = mostOfThisSize.count.ToString().Length;
            var cutOff = pdf.PageCount / 20;

            foreach (var (width, height, count) in sortedImageSizes.TakeWhile(x => x.width > 0 && x.count > cutOff))
            {
                Console.WriteLine($"  {count.ToString().PadLeft(padLen, ' ')}: {width} x {height}");
            }

            return pageMap.Values.First(p => p.LargestImage.width == mostOfThisSize.width && p.LargestImage.height == mostOfThisSize.height);
        }

        private static int? GetAdjustedHeightForOriginalImages(AbstractPdfPageInfo pageToSaveMostOfThisSize)
        {
            var adjustedHeight = pageToSaveMostOfThisSize.LargestImage.height;

            var maximumHeight = (Settings.MaximumHeight * 1.1d).ToInt(); // Add a little slack because we're saving original images
            if (adjustedHeight > maximumHeight)
            {
                adjustedHeight = maximumHeight;
            }

            return adjustedHeight;
        }

        private static int? GetAdjustedHeightForRendering(Pdf pdf, List<(int width, int height, int count)> sortedImageSizes, int dpiHeight)
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

        private static (int dpi, int wantedHeight) CalculateDpiForWantedWidth(Pdf pdf, AbstractPdfPageInfo pagwInfoWithWantedWidth)
        {
            Console.WriteLine($"Wanted width: {pagwInfoWithWantedWidth.LargestImage.width}");

            var pageMachine = new PopplerRenderPageMachine();

            int dpiHeight = 0;
            var dpiCalculator = new DpiCalculator(pageMachine, pdf, pagwInfoWithWantedWidth.LargestImage.width, pagwInfoWithWantedWidth.PageNumber);

            dpiCalculator.DpiCalculated += (s, e) =>
            {
                dpiHeight = e.Height;
                Console.WriteLine($"  {e.Dpi} -> {e.Width} x {dpiHeight}");
            };

            var dpi = dpiCalculator.CalculateDpi();

            DumpErrors(dpiCalculator.GetErrors());

            Console.WriteLine($"Selected dpi: {dpi}");
            return (dpi, dpiHeight);
        }

        private static async Task<int> ConvertPagesAsync(Pdf pdf, List<AbstractImageConverter> imageConverters)
        {
            // Each converter reads one page and renders it to memory or saves it to disk.
            // Each converter converts rendered images to jpg, or recompresses png images) in memory. Saved jps
            // The page compressor thread picks up converted images as they are saved (in page order) and writes them to the cbz file.

            // Key is pagenumber
            var convertedPages = new ConcurrentDictionary<int, AbstractImageConverter>(Settings.NumberOfThreads, pdf.PageCount);

            var pageCompressor = new PageCompressor(pdf, convertedPages);

            var pagesCompressed = 0;
            pageCompressor.PagesCompressed += (s, e) => OnPagesCompressed(e);

            await Parallel.ForEachAsync(imageConverters, new ParallelOptions { MaxDegreeOfParallelism = 1/*Settings.NumberOfThreads*/ }, async (converter, _) =>
            {
                converter.OpenImage();
                await converter.ConvertImageAsync();

                convertedPages.TryAdd(converter.GetPageNumber(), converter);
                pageCompressor.SignalPageConverted();
            });

            pageCompressor.SignalAllPagesConverted();
            pageCompressor.WaitForPagesCompressed();

            foreach (var imageProducer in imageConverters)
            {
                DumpErrors(imageProducer.GetErrorLines());
            }

            return pagesCompressed;

            void OnPagesCompressed(PagesCompressedEventArgs e)
            {
                pagesCompressed += e.Pages.Count();
            }
        }

        private static void DumpErrors(List<string> errorLines)
        {
            if (errorLines.Count == 0)
            {
                return;
            }

            var linesDict = new Dictionary<string, int>();

            foreach (var foundLine in errorLines)
            {
                linesDict[foundLine] = linesDict.TryGetValue(foundLine, out var count) ? count + 1 : 1;
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

            ProgressReporter.DumpErrors(lines);
        }
    }
}
