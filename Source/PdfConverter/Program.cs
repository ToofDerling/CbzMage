using CbzMage.Shared.Helpers;
using CoreComicsConverter.AppVersions;
using CoreComicsConverter.CbzCbrFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.PdfFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreComicsConverter
{
    public static class Program
    {
#if DEBUG
        private static readonly string[] _testArgs = new[] { @"M:\Data\Pdf\Test" };
#else
        private static readonly string[] _testArgs = null;
#endif
        public static void Main(string[] args)
        {
            if (_testArgs != null)
            {
                args = _testArgs;
            }

            try
            {
                var path = GetPath(args);
                if (path == null || !StartConvert(path))
                {
                    Console.WriteLine("CoreComicsConverter <directory|comic>");
                }
            }
            catch (PdfEncryptedException)
            {
                ProgressReporter.Error("Fatal error: pdf is encrypted!");
            }
            catch (Exception ex)
            {
                ProgressReporter.Error(ex.TypeAndMessage());
                ProgressReporter.Info(ex.StackTrace);
            }

            Console.ReadLine();
        }

        private static readonly ComicConverter converter = new ComicConverter();

        private static bool Convert(List<PdfComic> pdfComics)
        {
            if (Settings.Initialize(App.Ghostscript, App.SevenZip))
            {
                pdfComics.ForEach(comic => { converter.ConversionFlow(comic); });
            }
            return true;
        }

        private static bool Convert(List<CbzComic> cbzComics)
        {
            if (Settings.Initialize(App.SevenZip))
            {
                cbzComics.ForEach(comic => { converter.ConversionFlow(comic); });
            }
            return true;
        }

        private static bool StartConvert(string path)
        {
            var directory = new DirectoryInfo(path);
            if (directory.Exists)
            {
                return StartConvertDirectory(directory);

            }
            else if (File.Exists(path))
            {
                return StartConvertFile(path);
            }

            // Nothing to do
            return false;
        }

        private static bool StartConvertDirectory(DirectoryInfo directory, bool recursiveCall = false)
        {
            var entries = directory.GetFileSystemInfos();
            if (entries.Length == 0)
            {
                // Nothing to do
                return false;
            }

            if (entries.All(e => e.IsDirectory()))
            {
                if (recursiveCall)
                {
                    return false;
                }

                foreach (var entry in entries)
                {
                    var entryDirectory = new DirectoryInfo(entry.FullName);
                    return StartConvertDirectory(entryDirectory, recursiveCall: true);
                }
            }

            var files = entries.Select(e => e.FullName).ToArray();

            if (FilesAre(FileExt.Pdf, files))
            {
                return Convert(PdfComic.List(files));
            }

            if (FilesAre(FileExt.Cbz, files))
            {
                return Convert(CbzComic.List(files));
            }

            return false;
        }

        private static bool StartConvertFile(string file)
        {
            if (FilesAre(FileExt.Pdf, file))
            {
                return Convert(PdfComic.List(file));
            }

            if (FilesAre(FileExt.Cbz, file))
            {
                return Convert(CbzComic.List(file));
            }

            return false;
        }

        private static bool FilesAre(string ext, params string[] files)
        {
            return files.All(f => f.EndsWithIgnoreCase(ext));
        }

        private static string GetPath(string[] args)
        {
            string path = null;

            if (args.Length > 0)
            {
                path = args[0];
            }

            return !string.IsNullOrEmpty(path) ? path : null;
        }
    }
}
