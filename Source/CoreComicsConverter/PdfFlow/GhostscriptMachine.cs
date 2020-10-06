using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreComicsConverter.PdfFlow
{
    public class GhostscriptMachine
    {
        private static string GetSwitches(PdfComic pdfComic, int dpi, string pageList, string outputFile)
        {
            var args = new[]
            {
                "-sDEVICE=png16m",
                //"-dUseCIEColor",  //This adds a problematic icc profile to the image
                "-dTextAlphaBits=4",
                "-dGraphicsAlphaBits=4",
                "-dGridFitTT=2",
                "-dUseCropBox",
                //"-dMaxBitmap=1000000",
                //$"-dNumRenderingThreads={Settings.ParallelThreads}",
                $"-o {outputFile}",
                $"-sPageList={pageList}",
                $"-r{dpi}",
                $"\"{pdfComic.Path}\""
            };

            return string.Join(' ', args);
        }

        private static string CreatePageList(List<int> pageNumbers)
        {
            var sb = new StringBuilder();

            foreach (var p in pageNumbers)
            {
                sb.Append(p).Append(',');
            }
            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        public string GetReadPageString(int pageNumber)
        {
            return $"{pageNumber}-1{FileExt.Png}";
        }

        public void ReadPage(PdfComic pdfComic, int pageNumber, int dpi)
        {
            var switches = GetSwitches(pdfComic, dpi, pageNumber.ToString(), $"{pageNumber}-%d{FileExt.Png}");

            var ghostscriptRunner = new ProcessRunner();

            var errorLines = ghostscriptRunner.RunAndWaitForProcess(Settings.GhostscriptPath, switches, pdfComic.OutputDirectory, null);
            ProgressReporter.DumpErrors(errorLines);
        }

        public List<ComicPage> ReadPages(PdfComic pdfComic, ComicPageBatch batch, ProgressReporter reporter)
        {
            var pageNumbers = batch.Pages.Select(p => p.Number).AsList();
            var readPages = new List<ComicPage>(batch.Pages.Count);

            var pageList = CreatePageList(pageNumbers);
            var pageListId = $"{batch.FirstPage}-{batch.LastPage}";

            var switches = GetSwitches(pdfComic, batch.Dpi, pageList, $"{pageListId}-%d{FileExt.Png}");
            var pageQueue = GetPageQueue(pdfComic, batch.Pages, pageListId);

            var ghostscriptRunner = new ProcessRunner();

            var errorLines = ghostscriptRunner.RunAndWaitForProcess(Settings.GhostscriptPath, switches, pdfComic.OutputDirectory, OnOutputReceived);
            ProgressReporter.DumpErrors(errorLines);

            return readPages;

            void OnOutputReceived(object _, DataReceivedEventArgs e)
            {
                var line = e.Data;
                if (string.IsNullOrEmpty(line) || !line.StartsWith("Page"))
                {
                    return;
                }

                var page = pageQueue.Dequeue();

                reporter.ShowProgress($"Reading {page.Name}");

                readPages.Add(page);
            }
        }

        private static Queue<ComicPage> GetPageQueue(PdfComic pdfComic, IEnumerable<ComicPage> pages, string pageListId)
        {
            var pageQueue = new Queue<ComicPage>();

            int gsPageNumber = 1;
            foreach (var page in pages)
            {
                page.Name = $"{pageListId}-{gsPageNumber}{FileExt.Png}";
                page.Path = Path.Combine(pdfComic.OutputDirectory, page.Name);

                pageQueue.Enqueue(page);

                gsPageNumber++;
            }

            return pageQueue;
        }
    }
}