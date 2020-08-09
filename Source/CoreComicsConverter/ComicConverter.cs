using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Images;
using CoreComicsConverter.Model;
using CoreComicsConverter.PdfFlow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreComicsConverter
{
    public class ComicConverter
    {
        // Pdf conversion flow     
        public CompressCbzTask ConversionFlow(PdfComic pdfComic, CompressCbzTask compressCbzTask)
        {
            var stopWatch = Stopwatch.StartNew();
            Console.WriteLine(pdfComic.Path);

            var pdfFlow = new PdfConversionFlow();
            var pageSizes = pdfFlow.Initialize(pdfComic, this);

            var pageBatches = GetPageBatchesSortedByImageSize(pdfComic, pageSizes);
            pdfFlow.FixLargePageSize(pageBatches);

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

            StopStopwatch(stopWatch);

            return StartCompressPages(pdfComic);
        }

        public CompressCbzTask ConversionFlow(DirectoryComic directoryComic, CompressCbzTask compressCbzTask)
        {
            var stopWatch = Stopwatch.StartNew();
            Console.WriteLine(directoryComic.Path);

            var directoryFlow = new DirectoryConversionFlow();

            var isDownload = directoryFlow.IsDownload(directoryComic);
                
            if (isDownload && !directoryFlow.VerifyDownload(directoryComic))
            {
                return null;
            }

            var pageParser = new DirectoryImageParser(directoryComic);
            var pageSizes = ParseComic(directoryComic, pageParser);

            var pageBatches = GetPageBatchesSortedByImageSize(directoryComic, pageSizes);

            if (isDownload)
            {
                directoryFlow.FixDoublePageSpreads(pageBatches);
            }

            //var allreadPages = CalculateDpi(pdfComic, imageSizesList);
            //CoalescePageBatches(imageSizesList);

            StopStopwatch(stopWatch);

            return null;
        }

        public void StopStopwatch(Stopwatch stopwatch)
        {
            stopwatch.Stop();
            var passed = stopwatch.Elapsed;

            Console.WriteLine($"{passed.Minutes} min {passed.Seconds} sec");
            Console.WriteLine();
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

            ProgressReporter.Done($"FINISH {compressCbzTask.PdfComic.GetCbzName()}");
        }

        private static CompressCbzTask StartCompressPages(PdfComic pdfComic)
        {
            var compressCbzTask = new CompressCbzTask(pdfComic);

            Console.WriteLine($"START {compressCbzTask.PdfComic.GetCbzName()}");
            compressCbzTask.Start();

            return compressCbzTask;
        }

        public List<Page> ParseComic(Comic comic, IPageParser parser)
        {
            if (comic.PageCount == 0)
            {
                throw new ApplicationException("Comic pageCount is 0");
            }
            Console.WriteLine($"{comic.PageCount} pages");

            var progressReporter = new ProgressReporter(comic.PageCount);
            parser.PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page {e.Page.Number}");

            var pageSizes = parser.ParsePages();

            Console.WriteLine();
            Console.WriteLine($"{comic.ImageCount} images");

            return pageSizes;
        }

        private static List<PageBatch> GetPageBatchesSortedByImageSize(Comic comic, List<Page> pageSizes)
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
                throw new ApplicationException($"{nameof(pageBatches)} pagesCount is {pagesCount} should be {comic.PageCount}");
            }

            return pageBatches;
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
