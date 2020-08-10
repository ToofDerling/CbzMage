using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using CoreComicsConverter.PdfFlow;
using ImageMagick;
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
        private readonly Stopwatch _stopwatch = new Stopwatch();

        // Pdf conversion flow     
        public CreateOutputFileTask ConversionFlow(PdfComic pdfComic, CreateOutputFileTask outputFileTask)
        {
            StartConversion(pdfComic);

            var pdfFlow = new PdfConversionFlow();

            var pageSizes = pdfFlow.ParseImages(pdfComic);

            var pageBatches = GetPageBatchesSortedByImageSize(pdfComic, pageSizes);
            pdfFlow.FixLargePageSize(pageBatches);

            pageBatches = pdfFlow.CalculateDpi(pdfComic, pageBatches, out var readyPages);
            VerifyPageBatches(pdfComic, readyPages, pageBatches);

            pageBatches = pdfFlow.CoalescePageBatches(pageBatches);
            VerifyPageBatches(pdfComic, readyPages, pageBatches);

            var chunkedPageBatches = pdfFlow.ChunkPageBatches(pageBatches);
            VerifyPageBatches(pdfComic, readyPages, chunkedPageBatches);

            WaitForOutputFile(outputFileTask, onlyCheckIfCompleted: true);

            pdfFlow.ReadPages(pdfComic, chunkedPageBatches, readyPages);
            VerifyPageBatches(pdfComic, readyPages, chunkedPageBatches);

            WaitForOutputFile(outputFileTask, onlyCheckIfCompleted: true);

            ConvertPages(pdfComic, readyPages);

            StopConversion();

            WaitForOutputFile(outputFileTask);
            return StartOutputFileCreation(pdfComic);
        }

        // Directory conversion flow
        public CreateOutputFileTask ConversionFlow(DirectoryComic directoryComic, CreateOutputFileTask outputFileTask)
        {
            StartConversion(directoryComic);

            var directoryFlow = new DirectoryConversionFlow();

            var isDownload = directoryFlow.IsDownload(directoryComic);
            if (isDownload && !directoryFlow.VerifyDownload(directoryComic))
            {
                return null;
            }

            var pageSizes = directoryFlow.ParseImages(directoryComic);

            var pageBatches = GetPageBatchesSortedByImageSize(directoryComic, pageSizes);
            if (isDownload)
            {
                directoryFlow.FixDoublePageSpreads(pageBatches);
            }

            var readyPages = directoryFlow.GetPagesToConvert(pageBatches);
            if (readyPages.Count > 0)
            {
                WaitForOutputFile(outputFileTask, onlyCheckIfCompleted: true);

                ConvertPages(directoryComic, readyPages);
            }

            StopConversion();

            WaitForOutputFile(outputFileTask);
            return StartOutputFileCreation(directoryComic);
        }

        public void StartConversion(Comic comic)
        {
            _stopwatch.Restart();
            Console.WriteLine(comic.Path);
        }

        public void StopConversion()
        {
            _stopwatch.Stop();
            var passed = _stopwatch.Elapsed;

            Console.WriteLine($"{passed.Minutes} min {passed.Seconds} sec");
            Console.WriteLine();
        }

        public void WaitForOutputFile(CreateOutputFileTask outputFileTask, bool onlyCheckIfCompleted = false)
        {
            if (outputFileTask == null || outputFileTask.Comic.OutputFileCreated)
            {
                return;
            }

            if (!outputFileTask.IsCompleted)
            {
                if (onlyCheckIfCompleted)
                {
                    return;
                }

                Console.WriteLine($"WAIT {outputFileTask.Comic.OutputFile}");
                outputFileTask.Wait();
            }

            outputFileTask.Comic.CleanOutputDirectory();
            outputFileTask.Comic.OutputFileCreated = true;

            ProgressReporter.Done($"FINISH {outputFileTask.Comic.OutputFile}");
        }

        private static CreateOutputFileTask StartOutputFileCreation(Comic comic)
        {
            var outputFileTask = new CreateOutputFileTask(comic);

            Console.WriteLine($"START {outputFileTask.Comic.OutputFile}");
            outputFileTask.Start();

            return outputFileTask;
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

                pageBatches.Add(new PageBatch { Width = size.Key.Width, Height = size.Key.Height, Pages = size.AsList() });
            }

            var pagesCount = pageBatches.Sum(i => i.Pages.Count);
            if (pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(pageBatches)} pagesCount is {pagesCount} should be {comic.PageCount}");
            }

            return pageBatches;
        }

        private static void ConvertPages(Comic comic, ConcurrentQueue<Page> readyPages)
        {
            var progressReporter = new ProgressReporter(readyPages.Count);

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (readyPages.TryDequeue(out var page))
                {
                    var jpg = ConvertPage(comic, page);

                    progressReporter.ShowProgress($"Converted {jpg}");
                }
            });

            Console.WriteLine();

            if (readyPages.Count > 0)
            {
                throw new ApplicationException($"{nameof(readyPages)} is {readyPages.Count} should be 0");
            }
        }

        private static string ConvertPage(Comic comic, Page page)
        {
            using var image = new MagickImage(page.Path)
            {
                Format = MagickFormat.Jpg,
                Interlace = Interlace.Plane,
                Quality = Settings.JpegQuality
            };

            var jpg = comic.GetJpgPageString(page.Number);
            var jpgPath = Path.Combine(comic.OutputDirectory, jpg);

            if (page.NewWidth > 0 && page.NewHeight > 0)
            {
                image.Resize(page.NewWidth, page.NewHeight);
            }

            image.Write(jpgPath);
            File.Delete(page.Path);

            return jpg;
        }


        private static void VerifyPageBatches(Comic comic, ConcurrentQueue<Page> allReadPages, params List<PageBatch>[] pageBatches)
        {
            var pagesCount = pageBatches.Sum(batch => batch.Sum(i => i.Pages.Count));
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
    }
}
