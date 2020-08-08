using CoreComicsConverter.Extensions;
using CoreComicsConverter.Images;
using CoreComicsConverter.Model;
using CoreComicsConverter.PdfFlow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreComicsConverter
{
    public class ComicConverter
    {
        public CompressCbzTask ConvertToCbz(Comic comic, CompressCbzTask compressCbzTask)
        {
            Console.WriteLine(comic.Path);

            var pageSizes = ParseComic(comic);
            var pageBatches = CreatePageSizesList(comic, pageSizes);

            switch (comic.Type)
            {
                case ComicType.Pdf:
                    return ConversionFlow((PdfComic)comic, pageBatches, compressCbzTask);
                case ComicType.Directory:
                    ConversionFlow((DirectoryComic)comic, pageBatches);
                    break;
                default:
                    throw new ApplicationException($"Invalid type {comic.Type}");
            }

            return null;
        }

        // Pdf conversion flow     
        private CompressCbzTask ConversionFlow(PdfComic pdfComic, List<PageBatch> pageBatches, CompressCbzTask compressCbzTask)
        {
            pdfComic.CreateOutputDirectory();

            var pdfFlow = new PdfConversionFlow();

            pageBatches = pdfFlow.CalculateDpi(pdfComic, pageBatches, out var allReadPages);
            VerifyPageBatches(pdfComic, allReadPages, pageBatches);

            pageBatches = pdfFlow.CoalescePageBatches(pageBatches);
            VerifyPageBatches(pdfComic, allReadPages, pageBatches);

            var chunkedPageBatches = pdfFlow.ChunkPageBatches(pageBatches);
            VerifyPageBatches(pdfComic, allReadPages, chunkedPageBatches);

            WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            pdfFlow.ReadPages(pdfComic, chunkedPageBatches, allReadPages);
            VerifyPageBatches(pdfComic, allReadPages, chunkedPageBatches);

            WaitForCompressPages(compressCbzTask, onlyCheckIfCompleted: true);

            pdfFlow.ConvertPages(pdfComic, allReadPages);

            WaitForCompressPages(compressCbzTask);

            return StartCompressPages(pdfComic);
        }

        private void ConversionFlow(DirectoryComic pdfComic, List<PageBatch> imageSizesList)
        {
            //pdfComic.CreateOutputDirectory();
            //var allreadPages = CalculateDpi(pdfComic, imageSizesList);
            //CoalescePageBatches(imageSizesList);
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

        private static List<Page> ParseComic(Comic comic)
        {
            using ImageParser parser = ImageParserFactory.CreateFrom(comic);

            parser.OpenComicSetPageCount();
            Console.WriteLine($"{comic.PageCount} pages");

            var progressReporter = new ProgressReporter(comic.PageCount);
            parser.PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page {e.Page.Number}");

            var pageSizes = parser.ParsePagesSetImageCount();

            Console.WriteLine();
            Console.WriteLine($"{comic.ImageCount} images");

            var parserErrors = parser.GetParserWarnings();
            parserErrors.ForEach(warning => Console.WriteLine(warning));

            return pageSizes;
        }

        private static List<PageBatch> CreatePageSizesList(Comic comic, List<Page> pageSizes)
        {
            // Group the pages by imagesize and sort with largest size first
            var sizeLookup = pageSizes.ToLookup(p => (p.Width, p.Height)).OrderByDescending(i => i.Key.Width * i.Key.Height);

            var pageBatches = new List<PageBatch>();

            // Flatten the lookup
            foreach (var size in sizeLookup)
            {
                var pageNumbers = size.Select(s => s.Number).AsList();

                pageBatches.Add(new PageBatch { Width = size.Key.Width, Height = size.Key.Height, PageNumbers = pageNumbers });
            }

            var pagesCount = pageBatches.Sum(i => i.PageNumbers.Count);
            if (pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"imageSizesList pagesCount is {pagesCount} should be {comic.PageCount}");
            }

            // Cannot fix a single page comic
            if (pageSizes.Count > 1)
            {
                FixLargePageSize();
            }

            return pageBatches;

            void FixLargePageSize()
            {
                // If there's a single page much larger than the rest assume it's an error and try to fix it. 
                var largest = pageBatches[0];
                if (largest.PageNumbers.Count == 1)
                {
                    var secondLargest = pageBatches[1];
                    if (largest.Width > secondLargest.Width * 2 && largest.Height > secondLargest.Height * 2)
                    {
                        var oldHeight = largest.Height;
                        largest.Height = secondLargest.Height;

                        var factor = (double)oldHeight / largest.Height;

                        var oldWidth = largest.Width;
                        var newWidth = oldWidth / factor;
                        largest.Width = Convert.ToInt32(newWidth);

                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Fixed page {largest.FirstPage}: {oldWidth} x {oldHeight} -> {largest.Width} x {largest.Height}");
                        Console.ForegroundColor = oldColor;
                    }
                }
            }
        }
     
        private static void VerifyPageBatches(Comic comic, ConcurrentQueue<Page> allReadPages, params List<PageBatch>[] pageBatches)
        {
            var pagesCount = pageBatches.Sum(batch => batch.Sum(i => i.PageNumbers.Count));
            if (allReadPages.Count + pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(pageBatches)} pages is {pagesCount} should be {comic.PageCount - allReadPages.Count}");
            }
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

        private static void ConvertPages(PdfComic pdfComic, ConcurrentQueue<Page> allReadPages)
        {
            var progressReporter = new ProgressReporter(pdfComic.PageCount);

            var pageConverter = new PageConverter();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (allReadPages.TryDequeue(out var page))
                {
                    var pngPath = Path.Combine(pdfComic.OutputDirectory, page.Name);

                    var jpg = pdfComic.GetJpgPageString(page.Number);
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
