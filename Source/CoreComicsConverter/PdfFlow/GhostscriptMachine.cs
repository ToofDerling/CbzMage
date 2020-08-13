using CoreComicsConverter.Events;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                "-dNOPAUSE",
                "-dBATCH",
                "-sDEVICE=png16m",
                "-dUseCIEColor",
                "-dTextAlphaBits=4",
                "-dGraphicsAlphaBits=4",
                "-dGridFitTT=2",
                "-dUseCropBox",
                //"-dMaxBitmap=1000000",
                //$"-dNumRenderingThreads={Settings.ParallelThreads}",
                $"-sOutputFile={outputFile}",
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

        public void ReadPage(PdfComic pdfComic, int pageNumber, int dpi)
        {
            var padLen = pdfComic.PageCountLength;

            var switches = GetSwitches(pdfComic, dpi, pageNumber.ToString(), $"{pageNumber.ToString().PadLeft(padLen, '0')}-%0{padLen}d{FileExt.Png}");

            RunGhostscript(new List<int>() { pageNumber }, pageNumber.ToString(), padLen, switches, pdfComic.OutputDirectory);
        }

        public void ReadPageList(PdfComic pdfComic, ComicPageBatch batch)
        {
            var pageNumbers = batch.Pages.Select(p => p.Number).AsList();

            var pageList = CreatePageList(pageNumbers);

            var padLen = pdfComic.PageCountLength;

            var pageListId = $"{batch.FirstPage.ToString().PadLeft(padLen, '0')}-{batch.LastPage.ToString().PadLeft(padLen, '0')}";

            var switches = GetSwitches(pdfComic, batch.Dpi, pageList, $"{pageListId}-%0{padLen}d{FileExt.Png}");

            RunGhostscript(pageNumbers, pageListId, padLen, switches, pdfComic.OutputDirectory);
        }

        private void RunGhostscript(IEnumerable<int> pageNumbers, string pageListId, int padLen, string gsSwitches, string gsWorkingDirectory)
        {
            var ghostscriptRunner = new ProcessRunner();

            var pageQueue = GetPageQueue(pageNumbers, pageListId, padLen);
            ghostscriptRunner.OutputReceived += (s, e) => OnOutputReceived(e);

            ghostscriptRunner.RunAndWaitForProcess(Settings.GhostscriptPath, gsSwitches, gsWorkingDirectory, ProcessPriorityClass.Idle);
            DumpErrors();

            void OnOutputReceived(DataReceivedEventArgs e)
            {
                var line = e.Data;
                if (string.IsNullOrEmpty(line) || !line.StartsWith("Page"))
                {
                    return;
                }

                var (name, number) = pageQueue.Dequeue();
                PageRead?.Invoke(this, new PageEventArgs(new ComicPage { Name = name, Number = number }));
            }

            void DumpErrors() => ghostscriptRunner.GetErrorLines().ForEach(line => ProgressReporter.Error(line));
        }

        private static Queue<(string name, int number)> GetPageQueue(IEnumerable<int> pageNumbers, string pageListId, int padLen)
        {
            var pageQueue = new Queue<(string name, int number)>();

            int gsPageNumber = 1;
            foreach (var pageNumber in pageNumbers)
            {
                pageQueue.Enqueue(($"{pageListId}-{gsPageNumber.ToString().PadLeft(padLen, '0')}{FileExt.Png}", pageNumber));

                gsPageNumber++;
            }

            return pageQueue;
        }

        public event EventHandler<PageEventArgs> PageRead;
    }
}