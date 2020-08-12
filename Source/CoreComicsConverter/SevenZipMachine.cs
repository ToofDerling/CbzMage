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

            var reader = new OutputLinePagesReader(PageCompressed, allPages);

            sevenZipRunner.RunAndWaitForProcess(Settings.SevenZipPath, switches, reader.ProgressLineRead, workingDirectory, ProcessPriorityClass.Idle);

            var errorLines = sevenZipRunner.GetErrorLines();
            errorLines.ForEach(line => ProgressReporter.Error(line));
        }

        private class OutputLinePagesReader
        {
            private List<ComicPage> _allPages;

            private event EventHandler<PageEventArgs> _pageCompressed;

            public OutputLinePagesReader(EventHandler<PageEventArgs> pageCompressed, List<ComicPage> allPages)
            {
                _allPages = allPages;

                _pageCompressed = pageCompressed;
            }

            private int _startIndex;

            public void ProgressLineRead(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                var lastIndex = _allPages.FindIndex(p => line.Contains(p.Name));
                if (lastIndex == -1)
                {
                    return;
                }

                for (int i = _startIndex; i <= lastIndex; i++)
                {
                    _pageCompressed?.Invoke(this, new PageEventArgs(_allPages[i]));
                }

                _startIndex = lastIndex + 1;
            }
        }

        public event EventHandler<PageEventArgs> PageCompressed;
    }
}