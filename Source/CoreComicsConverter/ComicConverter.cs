using CoreComicsConverter.CbzCbrFlow;
using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using CoreComicsConverter.PdfFlow;
using ImageMagick;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoreComicsConverter
{
    public class ComicConverter
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        // Pdf conversion flow     
        public void ConversionFlow(PdfComic pdfComic)
        {
            ConversionBegin(pdfComic);

            var pdfFlow = new PdfConversionFlow();
            var readPages = new List<ComicPage>();

            var pageSizes = pdfFlow.ParseImagesSetPageCount(pdfComic);

            var pageSizeBatches = GetPageBatchesSortedByImageSize(pdfComic, pageSizes);
            VerifyPageBatches(pdfComic, readPages, pageSizeBatches);

            var pageBatch = pdfFlow.CalculateDpi(pdfComic, pageSizeBatches, readPages);
            VerifyPageBatches(pdfComic, readPages, pageBatch);

            var pageBatchChunks = pdfFlow.ChunkPageBatch(pageBatch);
            VerifyPageBatches(pdfComic, readPages, pageBatchChunks);

            pdfFlow.ReadPages(pdfComic, pageBatchChunks, readPages);
            VerifyPageBatches(pdfComic, readPages, pageBatchChunks);

            var changesWereMade = pdfFlow.AnalyzeImageSizes(readPages, pageBatch.Dpi, pageBatch.Height);

            var convertedPages = ConvertPages(pdfComic, readPages);

            if (changesWereMade)
            {
                pdfFlow.AnalyzeImageSizes(readPages, pageBatch.Dpi, pageBatch.Height);
            }

            CompressPages(pdfComic, convertedPages);

            ConversionEnd(pdfComic);
        }

        // Directory conversion flow
        public void ConversionFlow(DirectoryComic directoryComic)
        {
            ConversionBegin(directoryComic);

            var directoryFlow = new DirectoryConversionFlow();

            if (directoryComic.IsDownload && !directoryFlow.VerifyDownload(directoryComic))
            {
                return;
            }

            var pageSizes = directoryFlow.ParseImagesSetPageCount(directoryComic);
            var pageBatches = GetPageBatchesSortedByImageSize(directoryComic, pageSizes);

            if (directoryComic.IsDownload)
            {
                directoryFlow.FixDoublePageSpreads(pageBatches);
            }

            var pagesToConvert = directoryFlow.GetPagesToConvert(pageBatches);
            VerifyPageBatches(directoryComic, pagesToConvert, pageBatches);

            var convertedPages = ConvertPages(directoryComic, pagesToConvert);
            CompressPages(directoryComic, convertedPages);

            ConversionEnd(directoryComic);
        }

        public void ConversionFlow(CbzComic cbzComic)
        {
            ConversionBegin(cbzComic);

            var cbzFlow = new CbzCbrConversionFlow();

            cbzFlow.ExtractCbz(cbzComic);

            cbzFlow.CreatePdf(cbzComic);
        }

        private void ConversionBegin(Comic comic)
        {
            _stopwatch.Restart();

            Console.WriteLine(comic.Path);

            comic.CreateOutputDirectory();
        }

        private void ConversionEnd(Comic comic)
        {
            comic.CleanOutputDirectory();

            ProgressReporter.Done(comic.OutputFile);

            _stopwatch.Stop();
            var passed = _stopwatch.Elapsed;

            Console.WriteLine($"{passed.Minutes} min {passed.Seconds} sec");
            Console.WriteLine();
        }

        private static ComicPageBatch[] GetPageBatchesSortedByImageSize(Comic comic, List<ComicPage> pageSizes)
        {
            // Group the pages by imagesize and sort with largest size first
            var sizeLookup = pageSizes.ToLookup(p => (p.Width, p.Height)).OrderByDescending(i => i.Key.Width * i.Key.Height);

            var pageBatches = new List<ComicPageBatch>();

            // Flatten the lookup
            foreach (var size in sizeLookup)
            {
                var pageNumbers = size.Select(s => s.Number).AsList();

                pageBatches.Add(new ComicPageBatch { Width = size.Key.Width, Height = size.Key.Height, Pages = size.AsList() });
            }

            var pagesCount = pageBatches.Sum(i => i.Pages.Count);
            if (pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(pageBatches)} is {pagesCount} should be {comic.PageCount}");
            }

            return pageBatches.ToArray();
        }

        private static List<ComicPage> ConvertPages(Comic comic, List<ComicPage> readyPages)
        {
            var progressReporter = new ProgressReporter(readyPages.Count);

            var pagesQueue = new ConcurrentQueue<ComicPage>(readyPages);

            var convertedPagesBag = new ConcurrentBag<ComicPage>();

            var firstPage = readyPages[0];
            var deleteSource = Path.GetDirectoryName(firstPage.Path) == comic.OutputDirectory;

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (pagesQueue.TryDequeue(out var page))
                {
                    var jpg = ConvertPage(comic, page, deleteSource);

                    page.Name = jpg;

                    convertedPagesBag.Add(page);

                    progressReporter.ShowProgress($"Converting {jpg}");
                }
            });

            Console.WriteLine();

            var convertedPages = new List<ComicPage>(convertedPagesBag);

            if (!pagesQueue.IsEmpty)
            {
                throw new ApplicationException($"{nameof(pagesQueue)} is {pagesQueue.Count} should be 0");
            }

            if (convertedPages.Count != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(convertedPages)} is {convertedPages.Count} should be {comic.PageCount}");
            }

            return convertedPages;
        }

        private static string ConvertPage(Comic comic, ComicPage page, bool deleteSource)
        {
            using var image = new MagickImage(page.Path)
            {
                Format = MagickFormat.Jpg,
                Interlace = Interlace.Plane,
                Quality = Settings.JpegQuality
            };

            var jpg = comic.GetJpgPageString(page.Number);
            var jpgPath = Path.Combine(comic.OutputDirectory, jpg);

            if (page.NewHeight > 0)
            {
                image.Resize(0, page.NewHeight);
            }

            image.Write(jpgPath);

            if (deleteSource)
            {
                File.Delete(page.Path);
            }

            page.Path = jpgPath;

            return jpg;
        }

        private static void CompressPages(Comic comic, List<ComicPage> convertedPages)
        {
            File.Delete(comic.OutputFile);

            var machine = new SevenZipMachine();

            var reporter = new ProgressReporter(comic.PageCount);
            machine.PagesCompressed += (s, e) => OnPagesCompressed(e.Pages);

            convertedPages = convertedPages.OrderBy(p => p.Number).AsList();
            machine.CompressPages(comic, convertedPages);

            Console.WriteLine();

            void OnPagesCompressed(IReadOnlyCollection<ComicPage> pages)
            {
                foreach (var page in pages)
                {
                    reporter.ShowProgress($"Compressing {page.Name}");
                }
            }
        }

        private static void VerifyPageBatches(Comic comic, List<ComicPage> readPages, params ComicPageBatch[] pageBatches)
        {
            var pagesCount = pageBatches.Sum(batch => batch.Pages.Count);

            if (readPages.Count + pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(pageBatches)} pages is {pagesCount} should be {comic.PageCount - readPages.Count}");
            }
        }
    }
}
