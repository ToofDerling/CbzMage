using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreComicsConverter.CbzFlow
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

        public void CompressFile(Comic comic, List<string> convertedPages)
        {
            RunSevenZip(GetCompressSwitches(comic), comic.OutputDirectory, convertedPages);
        }

        private void RunSevenZip(string switches, string workingDirectory, List<string> convertedPages)
        {
            var sevenZipRunner = new ProcessRunner();

            var reader = new OutputLinePagesReader(PageCompressed, convertedPages);

            sevenZipRunner.RunAndWaitForProcess(Settings.SevenZipPath, switches, reader.ProgressLineRead, workingDirectory, ProcessPriorityClass.Idle);

            var errorLines = sevenZipRunner.GetErrorLines();
            errorLines.ForEach(line => ProgressReporter.Error(line));
        }

        private class OutputLinePagesReader
        {
            private List<string> _convertedPages;

            private event EventHandler<PageEventArgs> _pageCompressed;

            public OutputLinePagesReader(EventHandler<PageEventArgs> pageCompressed, List<string> convertedPages)
            {
                _convertedPages = convertedPages;

                _pageCompressed = pageCompressed;
            }

            private int _startIndex;

            public void ProgressLineRead(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                var lastIndex = _convertedPages.FindIndex(f => line.Contains(f));
                if (lastIndex == -1)
                {
                    return;
                }

                for (int i = _startIndex; i <= lastIndex; i++)
                {
                    var page = new ComicPage { Name = _convertedPages[i] };

                    _pageCompressed?.Invoke(this, new PageEventArgs(page));
                }

                _startIndex = lastIndex + 1;
            }
        }

        public event EventHandler<PageEventArgs> PageCompressed;
    }
}