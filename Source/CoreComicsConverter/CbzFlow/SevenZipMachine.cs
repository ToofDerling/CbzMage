using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using CoreComicsConverter.PdfFlow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CoreComicsConverter.CbzFlow
{
    public class SevenZipMachine
    {
        private static string GetSwitches(Comic comic)
        {
            var args = new[]
            {
                "x",
                $"\"{comic.Path}\"",
                $"-mmt{Settings.ParallelThreads}",
                $"-o\"{comic.OutputDirectory}\""
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

        public void ExtractFile(Comic comic)
        {
            var switches = GetSwitches(comic);

            ProcessHelper.RunAndWaitForProcess(switches, OutputLineRead, comic.OutputDirectory, ProcessPriorityClass.Idle);
        }

        public void ReadPageList(PdfComic pdfComic, ComicPageBatch batch)
        {
            var pageNumbers = batch.Pages.Select(p => p.Number).AsList();

            var pageList = CreatePageList(pageNumbers);

            var padLen = pdfComic.PageCountLength;

            var pageListId = $"{batch.FirstPage.ToString().PadLeft(padLen, '0')}-{batch.LastPage.ToString().PadLeft(padLen, '0')}";

            var switches = GetSwitches(pdfComic);

            var pageQueue = GetPageQueue(pageNumbers, pageListId, padLen);

            ProcessHelper.RunAndWaitForProcess(switches, OutputLineRead, pdfComic.OutputDirectory, ProcessPriorityClass.Idle);
        }

        private static Queue<(string name, int number)> GetPageQueue(IEnumerable<int> pageNumbers, string pageListId, int padLen)
        {
            var pageQueue = new Queue<(string name, int number)>();

            int gsPageNumber = 1;
            foreach (var pageNumber in pageNumbers)
            {
                pageQueue.Enqueue(($"{pageListId}-{gsPageNumber.ToString().PadLeft(padLen, '0')}.png", pageNumber));

                gsPageNumber++;
            }

            return pageQueue;
        }

        private void OutputLineRead(string line)
        {
            /*if (line == null || !line.StartsWith("Page"))
            {
                return;
            }*/

            PageRead?.Invoke(this, new PageEventArgs(new ComicPage { Name = line }));
        }

        public event EventHandler<PageEventArgs> PageRead;
    }
}