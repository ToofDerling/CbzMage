using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CoreComicsConverter.PdfFlow
{
    public class GhostscriptPageMachine
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

            pageNumbers.ForEach(p => sb.Append(p).Append(','));
            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        public void ReadPage(PdfComic pdfComic, int pageNumber, int dpi)
        {
            var padLen = pdfComic.PageCountLength;

            var switches = GetSwitches(pdfComic, dpi, pageNumber.ToString(), $"{pageNumber.ToString().PadLeft(padLen, '0')}-%0{padLen}d.png");

            var pageQueue = GetPageQueue(new List<int>() { pageNumber }, pageNumber.ToString(), padLen);

            RunAndWaitForProcess(pdfComic, switches, pageQueue, ProcessPriorityClass.Idle);
        }

        public void ReadPageList(PdfComic pdfComic, PageBatch batch)
        {
            var pageList = CreatePageList(batch.PageNumbers);

            var padLen = pdfComic.PageCountLength;

            var pageListId = $"{batch.FirstPage.ToString().PadLeft(padLen, '0')}-{batch.LastPage.ToString().PadLeft(padLen, '0')}";

            var switches = GetSwitches(pdfComic, batch.Dpi, pageList, $"{pageListId}-%0{padLen}d.png");

            var pageQueue = GetPageQueue(batch.PageNumbers, pageListId, padLen);

            RunAndWaitForProcess(pdfComic, switches, pageQueue, ProcessPriorityClass.Idle);
        }

        private static Queue<(string name, int number)> GetPageQueue(List<int> pageNumbers, string pageListId, int padLen)
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

        private void OutputLineRead(Queue<(string name, int number)> pageQueue, string line)
        {
            if (line == null || !line.StartsWith("Page"))
            {
                return;
            }

            var (name, number) = pageQueue.Dequeue();

            PageRead?.Invoke(this, new PageEventArgs(new Page { Name = name, Number = number }));
        }

        public event EventHandler<PageEventArgs> PageRead;

        private void RunAndWaitForProcess(PdfComic pdfComic, string switches, Queue<(string name, int number)> pageQueue, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
        {
            using var process = GetGSProcess(switches, pdfComic.OutputDirectory);

            process.OutputDataReceived += (s, e) => OutputLineRead(pageQueue, e.Data);

            process.Start();

            process.PriorityClass = priorityClass;

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
        }

        public static Process GetGSProcess(string args, string outputDirectory)
        {
            var process = new Process();

            process.StartInfo.FileName = Settings.GhostscriptPath;
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