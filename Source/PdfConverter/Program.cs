using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using PdfConverter.AppVersions;
using PdfConverter.PdfFlow;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfConverter
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

        private static readonly PdfConverter pdfConverter = new();

        private static bool Convert(List<PdfComic> pdfComics)
        {
            if (Settings.Initialize(App.Ghostscript, App.SevenZip))
            {
                pdfComics.ForEach(comic => { pdfConverter.ConversionFlow(comic); });
            }
            return true;
        }

        private static bool StartConvert(string path)
        {
            if (Directory.Exists(path))
            {
                return StartConvertDirectory(path);

            }
            else if (File.Exists(path))
            {
                return StartConvertFile(path);
            }

            // Nothing to do
            return false;
        }

        private static bool StartConvertDirectory(string directory)
        {
            var pdfFiles = Directory.GetFiles(directory, "*.pdf");
            if (pdfFiles.Length == 0)
            {
                // Nothing to do
                return false;
            }

            return Convert(PdfComic.List(pdfFiles));
        }

        private static bool StartConvertFile(string file)
        {
            if (file.EndsWithIgnoreCase(".pdf"))
            {
                return Convert(PdfComic.List(file));
            }

            return false;
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
