using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CoreComicsConverter
{
    public class GhostscriptPageMachine
    {
        private static string GetSwitches(PdfComic pdfComic, int dpi, string pageList, string outputFile)
        {
            var args = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -sOutputFile={outputFile} -sPageList={pageList} -r{dpi} \"{pdfComic.Path}\"";
            return args;
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
            var switches = GetSwitches(pdfComic, dpi, pageNumber.ToString(), $"{pageNumber}-%d.png");

            var pageQueue = GetPageQueue(new List<int>() { pageNumber }, pageNumber.ToString());

            using var process = GetGSProcess(switches, pdfComic.OutputDirectory);

            process.OutputDataReceived += (s, e) => OutputLineRead(pageQueue, e.Data);

            RunAndWaitForProcess(process);
        }

        public void ReadPageList(PdfComic pdfComic, Pages pageBatch)
        {
            var pageList = CreatePageList(pageBatch.PageNumbers);

            int padLen = pdfComic.PageCount.ToString().Length;
            var pageListId = $"{pageBatch.PageNumbers.First().ToString().PadLeft(padLen, '0')}-{pageBatch.PageNumbers.Last().ToString().PadLeft(padLen, '0')}";

            var switches = GetSwitches(pdfComic, pageBatch.Dpi, pageList, $"{pageListId}-%d{padLen}.png");

            var pageQueue = GetPageQueue(pageBatch.PageNumbers, pageListId);

            using var process = GetGSProcess(switches, pdfComic.OutputDirectory);

            process.OutputDataReceived += (s, e) => OutputLineRead(pageQueue, e.Data);

            RunAndWaitForProcess(process, ProcessPriorityClass.Idle);
        }

        private static Queue<(string name, int number)> GetPageQueue(List<int> pageNumbers, string pageListId)
        {
            var pageQueue = new Queue<(string name, int number)>();

            int gsPageNumber = 1;
            foreach (var pageNumber in pageNumbers)
            {
                pageQueue.Enqueue(($"{pageListId}-{gsPageNumber}.png", pageNumber));

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

            PageRead?.Invoke(this, new PageEventArgs(name, number));
        }

        public event EventHandler<PageEventArgs> PageRead;


        private static void RunAndWaitForProcess(Process process, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
        {
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