using CoreComicsConverter.Events;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreComicsConverter
{
    public class SevenZipMachine
    {
        private static string GetExtractSwitches(Comic comic)
        {
            var args = new[]
            {
                "x",
                $"\"{comic.Path}\"",
                $"-mmt{Settings.ParallelThreads}",
                "-y",
                $"-o\"{comic.OutputDirectory}\""
            };

            return string.Join(' ', args);
        }

        private static string GetCompressSwitches(Comic comic)
        {
            var args = new[]
            {
                "a",
                $"\"{comic.OutputFile}\"",
                $"-mmt{Settings.ParallelThreads}",
                "-y",
                "-tzip",
                "-mx=7",
                "-bsp1",
                "-bso0",
                $"-o\"{comic.OutputDirectory}\""
            };

            return string.Join(' ', args);
        }

        public void ExtractFile(Comic comic)
        {
            RunSevenZip(GetExtractSwitches(comic), comic.OutputDirectory, null);
        }

        public void CompressFile(Comic comic, List<ComicPage> allPages)
        {
            RunSevenZip(GetCompressSwitches(comic), comic.OutputDirectory, allPages);
        }

        private void RunSevenZip(string switches, string workingDirectory, List<ComicPage> allPages)
        {
            var sevenZipRunner = new ProcessRunner();

            var allPagesStartIndex = 0;
            sevenZipRunner.OutputReceived += (s, e) => OnOutputReceived(e);

            sevenZipRunner.RunAndWaitForProcess(Settings.SevenZipPath, switches, workingDirectory, ProcessPriorityClass.Idle);

            var errorLines = sevenZipRunner.GetErrorLines();
            errorLines.ForEach(line => ProgressReporter.Error(line));

            void OnOutputReceived(DataReceivedEventArgs e)
            {
                var line = e.Data;
                int endIndex;

                if (string.IsNullOrWhiteSpace(line) || (endIndex = allPages.FindIndex(p => line.Contains(p.Name))) == -1)
                {
                    return;
                }

                allPagesStartIndex = InvokePagesCompressed(allPages, allPagesStartIndex, endIndex);
            }
        }

        private int InvokePagesCompressed(List<ComicPage> allPages, int startIndex, int endIndex)
        {
            var compressedPages = new List<ComicPage>();

            for (int i = startIndex; i <= endIndex; i++)
            {
                compressedPages.Add(allPages[i]);
            }

            PagesCompressed?.Invoke(this, new PagesEventArgs(compressedPages));

            return endIndex + 1;
        }

        public event EventHandler<PagesEventArgs> PagesCompressed;
    }
}