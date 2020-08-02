using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CoreComicsConverter
{
    public class GhostscriptPageMachine
    {
        private static string GetSwitches(PdfComic pdf, int dpi, string pageList, string outputFile)
        {
            var args = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -sOutputFile={outputFile} -sPageList={pageList} -r{dpi} \"{pdf.PdfPath}\"";
            return args;
        }

        private static string CreatePageList(List<int> pageNumbers)
        {
            var sb = new StringBuilder();

            pageNumbers.ForEach(p => sb.Append(p).Append(','));
            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        public void ReadFirstPage(PdfComic pdf, int dpi)
        {
            var switches = GetSwitches(pdf, dpi, "1", $"page-%0{pdf.PageCountLength}d.png");

            using var process = GetGSProcess(switches, pdf.OutputDirectory);

            RunAndWaitForProcess(process);
        }

        public void ReadPageList(PdfComic pdf, List<int> pageNumbers, int pageListId, int dpi)
        {
            var pageList = CreatePageList(pageNumbers);

            var switches = GetSwitches(pdf, dpi, pageList, $"{pageListId}-%0{pdf.PageCountLength}d.png");

            var pageQueue = GetPageQueue(pageNumbers, pageListId, pdf.PageCountLength);

            using var process = GetGSProcess(switches, pdf.OutputDirectory);

            process.OutputDataReceived += (s, e) => OutputLineRead(pageQueue, e.Data);

            RunAndWaitForProcess(process, ProcessPriorityClass.Idle);
        }

        private static Queue<(string name, int number)> GetPageQueue(List<int> pageNumbers, int pageListId, int pageLength)
        {
            var pageQueue = new Queue<(string name, int number)>();

            int gsPageNumber = 1;
            foreach (var pageNumber in pageNumbers)
            {
                var pageString = gsPageNumber.ToString().PadLeft(pageLength, '0');

                pageQueue.Enqueue(($"{pageListId}-{pageString}.png", pageNumber));

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