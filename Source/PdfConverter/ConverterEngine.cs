﻿using CbzMage.Shared.Buffers;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using PdfConverter.Exceptions;
using PdfConverter.Ghostscript;
using PdfConverter.ImageProducer;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class ConverterEngine
    {
        public void ConvertToCbz(Pdf pdf, PdfImageParser pdfParser)
        {
            var sortedImageSizes = ParsePdfImages(pdf, pdfParser);

            // Use original images from pdf
            if (SavePdfImages(pdf, pdfParser, sortedImageSizes))
            {
                return;
            }

            var wantedWidth = GetWantedWidth(pdf, sortedImageSizes);

            var (dpi, dpiHeight) = CalculateDpiForWantedWidth(pdf, wantedWidth);
            var adjustedHeight = GetAdjustedHeight(pdf, sortedImageSizes, dpiHeight);

            var pageLists = CreatePageLists(pdf);
            var imageProducers = new List<AbstractImageProducer>();

            foreach (var pageList in pageLists)
            {
                var producer = new GhostScriptImageProducer(pdf, pageList, dpi);
                imageProducers.Add(producer);
            }

            var fileCount = ConvertPages(pdf, imageProducers, adjustedHeight);

            if (!Settings.SaveCoverOnly && (fileCount != pdf.PageCount))
            {
                throw new SomethingWentWrongSorryException($"{fileCount} files generated for {pdf.PageCount} pages");
            }
        }

        private static List<(int width, int height, int count)> ParsePdfImages(Pdf pdf, PdfImageParser pdfImageParser)
        {
            var progressReporter = new ProgressReporter(pdf.PageCount);

            pdfImageParser.PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page-{e.CurrentPage}");

            var sortedImageSizes = pdfImageParser.ParseImages();
            progressReporter.EndProgress();

            Console.WriteLine($"{pdf.ImageCount} images");

            var parserErrors = pdfImageParser.GetImageParserErrors();
            parserErrors.ForEach(ex => Console.WriteLine(ex.TypeAndMessage()));

            return sortedImageSizes;
        }

        private static bool SavePdfImages(Pdf pdf, PdfImageParser pdfImageParser, List<(int width, int height, int count)> sortedImageSizes)
        {
            Console.Write("Use original images: ");

            // Saving original images from pdf requires each page has exactly one image
            if (pdf.ImageCount != pdf.PageCount
                // Detect page without an image.
                || sortedImageSizes.Any(i => i.width == 0))
            {
                ProgressReporter.Info("not available");
                return false;
            }

            try
            {
                // Disable saving original images if pdf contains any text to render
                if (pdfImageParser.DetectRenderedText())
                {
                    ProgressReporter.Info("not available");
                    return false;
                }
                ProgressReporter.Done("ok");
            }
            catch (IOException)
            {
                // Text rendering can fail because of a missing font
                ProgressReporter.Info("error");
                return false;
            }

            var pageLists = CreatePageLists(pdf);
            var imageProducers = new List<AbstractImageProducer>();

            foreach (var pageList in pageLists)
            {
                var producer = new ITextImageProducer(pdf, pageList);
                imageProducers.Add(producer);
            }

            ConvertPages(pdf, imageProducers, resizeHeight: null);

            return true;
        }

        private static int GetWantedWidth(Pdf pdf, List<(int width, int height, int count)> sortedImageSizes)
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

        private static int? GetAdjustedHeight(Pdf pdf, List<(int width, int height, int count)> sortedImageSizes, int dpiHeight)
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

        private static (int dpi, int wantedHeight) CalculateDpiForWantedWidth(Pdf pdf, int wantedImageWidth)
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

            var (foundErrors, warningsOrErrors) = dpiCalculator.WarningsOrErrors;
            if (warningsOrErrors.Count > 0)
            {
                var isWarnings = foundErrors == 0;
                DumpWarningsOrErrors(isWarnings, warningsOrErrors);
            }

            Console.WriteLine($"Selected dpi: {dpi}");
            return (dpi, dpiHeight);
        }

        private static List<int>[] CreatePageLists(Pdf pdf)
        {
            // If we're only saving the cover this is all we need.
            if (Settings.SaveCoverOnly)
            {
                return new[] { new List<int> { 1 } };
            }

            var pageCount = pdf.PageCount;
            var maxThreads = Settings.NumberOfThreads;

            // The goal is to have no pagelist with only one page (unless pageCount is 1) 
            var parallelThreads = 1;
            for (; parallelThreads < maxThreads; parallelThreads++)
            {
                if ((pageCount / parallelThreads) < 4)
                {
                    break;
                }
            }

            var pageLists = PageChunker.CreatePageLists(pageCount, parallelThreads);

            Array.ForEach(pageLists, p => Console.WriteLine($"  Reader{p.First()}: {p.Count} pages"));

            return pageLists;
        }

        private static int ConvertPages(Pdf pdf, List<AbstractImageProducer> imageProducers, int? resizeHeight)
        {
            // Each image producer reads a range of pages continously and renders or saves the images in memory.
            // Each producer has a dedicated converter thread that converts images to jpg (or recompresses png images), also in memory. 
            // The page compressor thread picks up converted images as they are saved (in page order) and writes them to the cbz file.

            var progressReporter = new ProgressReporter(pdf.PageCount);

            // Key is pagenumber
            var convertedPages = new ConcurrentDictionary<int, (ArrayPoolBufferWriter<byte> imageData, string imageExt)>(imageProducers.Count, pdf.PageCount);

            var pageCompressor = new PageCompressor(pdf, convertedPages);

            var pagesCompressed = 0;
            pageCompressor.PagesCompressed += (s, e) => OnPagesCompressed(e);

            Parallel.ForEach(imageProducers, producer =>
            {
                var pageList = producer.PageList;
                var pageQueue = new Queue<int>(pageList);

                var pageConverter = new PageConverter(pageQueue, convertedPages, resizeHeight);
                pageConverter.PageConverted += (s, e) => pageCompressor.OnPageConverted(e);

                producer.Start(pageConverter);

                pageConverter.WaitForPagesConverted();
            });

            pageCompressor.SignalAllPagesConverted();
            pageCompressor.WaitForPagesCompressed();

            progressReporter.EndProgress();

            var foundErrors = 0;
            var warningsOrErrors = new List<string>();

            foreach (var producer in imageProducers)
            {
                foundErrors += producer.WaitForExit();
                warningsOrErrors.AddRange(producer.GetErrors());

                producer.Dispose();
            }

            if (warningsOrErrors.Count > 0)
            {
                var isWarnings = foundErrors == 0;
                DumpWarningsOrErrors(isWarnings, warningsOrErrors);
            }

            return pagesCompressed;

            void OnPagesCompressed(PagesCompressedEventArgs e)
            {
                pagesCompressed += e.Pages.Count();
            }
        }

        private static void DumpWarningsOrErrors(bool linesIsWarnings, List<string> warningsOrErrors)
        {
            var linesDict = new Dictionary<string, int>();

            foreach (var foundLine in warningsOrErrors)
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
