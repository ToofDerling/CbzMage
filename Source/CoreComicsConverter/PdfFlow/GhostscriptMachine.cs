using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System.Collections.Generic;
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

        public string GetReadPageString()
        {
            // ComicPageBatch.BatchIs starts at 1 so use 0 to denote the single page read
            return $"0-1{FileExt.Png}";
        }

        public void ReadPage(PdfComic pdfComic, int pageNumber, int dpi)
        {
            var switches = GetSwitches(pdfComic, dpi, pageNumber.ToString(), $"0-%d{FileExt.Png}");

            var errorLines = ProcessRunner.RunAndWaitForProcess(Settings.GhostscriptPath, switches, pdfComic.OutputDirectory, null);
            ProgressReporter.DumpErrors(errorLines);
        }

        public void ReadPages(PdfComic pdfComic, ComicPageBatch batch)
        {
            var pageNumbers = batch.Pages.Select(p => p.Number).AsList();
            var readPages = new List<ComicPage>(batch.Pages.Count);

            var pageList = CreatePageList(pageNumbers);

            var switches = GetSwitches(pdfComic, batch.Dpi, pageList, $"{batch.BatchId}-%d{FileExt.Png}");

            var errorLines = ProcessRunner.RunAndWaitForProcess(Settings.GhostscriptPath, switches, pdfComic.OutputDirectory, null);
            ProgressReporter.DumpErrors(errorLines);
        }

        public static IDictionary<string, ComicPage> GetPageMap(ComicPageBatch[] pageBatches)
        {
            var pageMap = new Dictionary<string, ComicPage>(pageBatches.Sum(b => b.Pages.Count));

            foreach (var batch in pageBatches)
            {
                var pages = batch.Pages;
                var batchId = batch.BatchId;

                for (int i = 0, psz = pages.Count; i < psz; i++)
                {
                    var gsPageNumber = i + 1;

                    var page = pages[i];
                    page.Name = $"{batchId}-{gsPageNumber}{FileExt.Png}";

                    pageMap[page.Name] = page;
                }
            }

            return pageMap;
        }
    }
}