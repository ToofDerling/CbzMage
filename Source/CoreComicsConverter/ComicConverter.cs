using CoreComicsConverter.CbzFlow;
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

            var pageSizes = pdfFlow.ParseImagesSetPageCount(pdfComic);

            var pageBatches = GetPageBatchesSortedByImageSize(pdfComic, pageSizes);
            pdfFlow.FixLargePageSize(pageBatches);

            pageBatches = pdfFlow.CalculateDpi(pdfComic, pageBatches, out var readyPages);
            VerifyPageBatches(pdfComic, readyPages, pageBatches);

            pageBatches = pdfFlow.CoalescePageBatches(pageBatches);
            VerifyPageBatches(pdfComic, readyPages, pageBatches);

            var chunkedPageBatches = pdfFlow.ChunkPageBatches(pageBatches);
            VerifyPageBatches(pdfComic, readyPages, chunkedPageBatches);

            pdfFlow.ReadPages(pdfComic, chunkedPageBatches, readyPages);
            VerifyPageBatches(pdfComic, readyPages, chunkedPageBatches);

            var convertedPages = ConvertPages(pdfComic, readyPages);

            pdfFlow.CompressPages(pdfComic, convertedPages);

            ConversionEnd();
        }

        // Directory conversion flow
        public void ConversionFlow(DirectoryComic directoryComic)
        {
            ConversionBegin(directoryComic);

            var directoryFlow = new DirectoryConversionFlow();

            var isDownload = directoryFlow.IsDownload(directoryComic);
            if (isDownload && !directoryFlow.VerifyDownload(directoryComic))
            {
                return;
            }

            var pageSizes = directoryFlow.ParseImagesSetPageCount(directoryComic);

            var pageBatches = GetPageBatchesSortedByImageSize(directoryComic, pageSizes);
            if (isDownload)
            {
                directoryFlow.FixDoublePageSpreads(pageBatches);
            }

            var readyPages = directoryFlow.GetPagesToConvert(pageBatches);
            VerifyPageBatches(directoryComic, readyPages, pageBatches);

            if (!readyPages.IsEmpty)
            {
                ConvertPages(directoryComic, readyPages);
            }

            ConversionEnd();
        }

        public void ConversionFlow(CbzComic cbzComic)
        {
            ConversionBegin(cbzComic);

            var cbzFlow = new CbzConversionFlow();

            cbzFlow.ExtractCbz(cbzComic);
        }

        public void ConversionBegin(Comic comic)
        {
            _stopwatch.Restart();
            Console.WriteLine(comic.Path);
        }

        public void ConversionEnd()
        {
            _stopwatch.Stop();
            var passed = _stopwatch.Elapsed;

            Console.WriteLine($"{passed.Minutes} min {passed.Seconds} sec");
            Console.WriteLine();
        }

        private static List<ComicPageBatch> GetPageBatchesSortedByImageSize(Comic comic, List<ComicPage> pageSizes)
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
                throw new ApplicationException($"{nameof(pageBatches)} pagesCount is {pagesCount} should be {comic.PageCount}");
            }

            return pageBatches;
        }

        private List<string> ConvertPages(Comic comic, ConcurrentQueue<ComicPage> readyPages)
        {
            var progressReporter = new ProgressReporter(readyPages.Count);

            var convertedPages = new ConcurrentBag<string>();

            Parallel.For(0, Settings.ParallelThreads, (index, state) =>
            {
                while (readyPages.TryDequeue(out var page))
                {
                    var jpg = ConvertPage(comic, page);

                    progressReporter.ShowProgress($"Converted {jpg}");

                    convertedPages.Add(jpg);

                }
            });

            Console.WriteLine();

            if (!readyPages.IsEmpty)
            {
                throw new ApplicationException($"{nameof(readyPages)} is {readyPages.Count} should be 0");
            }

            if (convertedPages.Count != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(convertedPages)} is {convertedPages.Count} should be {comic.PageCount}");
            }

            var list = new List<string>(convertedPages);
            list.Sort();

            return list;
        }

        private static string ConvertPage(Comic comic, ComicPage page)
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

        private static void VerifyPageBatches(Comic comic, ConcurrentQueue<ComicPage> readyPages, params List<ComicPageBatch>[] pageBatches)
        {
            var pagesCount = pageBatches.Sum(batch => batch.Sum(i => i.Pages.Count));

            if (readyPages.Count + pagesCount != comic.PageCount)
            {
                throw new ApplicationException($"{nameof(pageBatches)} pages is {pagesCount} should be {comic.PageCount - readyPages.Count}");
            }
        }
    }
}
