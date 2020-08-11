using CoreComicsConverter.Extensions;
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

            RunAndWaitForProcess(comic, switches, ProcessPriorityClass.Idle);
        }

        public void ReadPageList(PdfComic pdfComic, ComicPageBatch batch)
        {
            var pageNumbers = batch.Pages.Select(p => p.Number).AsList();

            var pageList = CreatePageList(pageNumbers);

            var padLen = pdfComic.PageCountLength;

            var pageListId = $"{batch.FirstPage.ToString().PadLeft(padLen, '0')}-{batch.LastPage.ToString().PadLeft(padLen, '0')}";

            var switches = GetSwitches(pdfComic);

            var pageQueue = GetPageQueue(pageNumbers, pageListId, padLen);

            RunAndWaitForProcess(pdfComic, switches, ProcessPriorityClass.Idle);
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

        private void RunAndWaitForProcess(Comic pdfComic, string switches, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
        {
            using var process = GetProcess(switches, pdfComic.OutputDirectory);

            process.OutputDataReceived += (s, e) => OutputLineRead(e.Data);

            process.Start();

            process.PriorityClass = priorityClass;

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
        }

        public static Process GetProcess(string args, string outputDirectory)
        {
            var process = new Process();

            process.StartInfo.FileName = Settings.SevenZipPath;
            process.StartInfo.Arguments = args;

            process.StartInfo.WorkingDirectory = outputDirectory;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            };

            return process;
        }
    }
}