using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using PdfConverter.Events;
using PdfConverter.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PdfConverter
{
    public class SevenZipMachine
    {
        private static string GetExtractSwitches(Comic comic)
        {
            var args = new[]
            {
                "x",
                $"\"{comic.Path}\"",
                $"-mmt={Settings.ParallelThreads}",
                "-y",
                "-bsp1",
                "-bso0",
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
                $"-mmt={Settings.ParallelThreads}",
                "-y",
                "-tzip",
                "-mx=7",
                "-bsp1",
                "-bso0",
                $"-o\"{comic.OutputDirectory}\""
            };

            return string.Join(' ', args);
        }

        public string[] ExtractComic(Comic comic)
        {
            var switches = GetExtractSwitches(comic);
            var lastProgress = string.Empty;

            var errorLines = ProcessRunner.RunAndWaitForProcess(Settings.SevenZipPath, switches, comic.OutputDirectory, OnExtractOutputReceived);

            if (!lastProgress.Contains("100%"))
            {
                Extracted?.Invoke(this, new ExtractedEventArgs("100%"));
            }

            ProgressReporter.DumpErrors(errorLines);

            return Directory.EnumerateFiles(comic.OutputDirectory, "*", SearchOption.AllDirectories)
                .Where(f => f.EndsWithIgnoreCase(FileExt.Jpg) || f.EndsWithIgnoreCase(FileExt.Png))
                .OrderBy(f => f).ToArray();

            void OnExtractOutputReceived(object _, DataReceivedEventArgs e)
            {
                var progress = e.Data;

                if (string.IsNullOrWhiteSpace(progress))
                {
                    return;
                }

                var idx = progress.IndexOf('%');
                if (idx == -1)
                {
                    return;
                }

                lastProgress = progress[..(idx + 1)].TrimStart();

                Extracted?.Invoke(this, new ExtractedEventArgs(lastProgress));
            }
        }

        public event EventHandler<ExtractedEventArgs> Extracted;

        public void CompressPages(Comic comic, List<ComicPage> allPages)
        {
            var switches = GetCompressSwitches(comic);
            var allPagesStartIndex = 0;

            var errorLines = ProcessRunner.RunAndWaitForProcess(Settings.SevenZipPath, switches, comic.OutputDirectory, OnCompressOutputReceived);
            ProgressReporter.DumpErrors(errorLines);

            void OnCompressOutputReceived(object _, DataReceivedEventArgs e)
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