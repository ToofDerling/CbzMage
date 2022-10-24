using CbzMage.Shared.Extensions;
using PdfConverter.Ghostscript;
using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System.Diagnostics;

namespace PdfConverter
{
    public class Program
    {

#if DEBUG
        private static readonly string _testPdf = @"M:\Data\Pdf\Test";
#else
        private static readonly string _testPdf = null;
#endif

        private static int pagesCount = 0;

        public static void Main(string[] args)
        {
            var config = new PdfSettings();
            config.CreateSettings();

            var pdfList = InitializePdfPath(args);
            if (!pdfList.Any())
            {
                Console.WriteLine("No pdf files found, bye");
            }

            var gsVersion = GhostscriptPageMachineManager.GetGhostscriptVersion();
            if (gsVersion == null)
            {
                return;
            }

            Console.WriteLine($"Using Ghostscript version: {gsVersion.Version}");
            Console.WriteLine($"Conversion mode: {Settings.PdfConverterMode}");
            Console.WriteLine($"Number of threads: {Settings.NumberOfThreads}");

            var stopwatch = Stopwatch.StartNew();

            using (var pageMachineManager = new GhostscriptPageMachineManager(gsVersion))
            {
                using var bufferCache = new BufferCache(Settings.BufferSize);

                var converter = new PdfComicConverter(pageMachineManager);
                pdfList.ForEach(pdf => ConvertPdf(pdf, converter));
            }

#if DEBUG
            StatsCount.ShowStats();
            Console.WriteLine();
#endif

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed;
            var secsPerPage = elapsed.TotalSeconds / pagesCount;

            Console.WriteLine($"{pagesCount} pages converted in {elapsed.Minutes} min {elapsed.Seconds} sec ({secsPerPage:F2} sec/page)");

            Console.ReadLine();
        }

        private static void ConvertPdf(Pdf pdf, PdfComicConverter converter)
        {
            var stopwatch = Stopwatch.StartNew();

            using var pdfParser = new PdfImageParser(pdf);

            Console.WriteLine(pdf.Path);
            Console.WriteLine($"{pdf.PageCount} pages");

            pagesCount += pdf.PageCount;

            converter.ConvertToCbz(pdf, pdfParser);

            stopwatch.Stop();
            var passed = stopwatch.Elapsed;

            var min = passed.Minutes > 0 ? $"{passed.Minutes} min " : string.Empty;
            var sec = passed.Seconds > 0 ? $"{passed.Seconds} sec" : string.Empty;

            Console.WriteLine($"{min}{sec}");
            Console.WriteLine();
        }

        private static List<Pdf> InitializePdfPath(string[] args)
        {
            var path = (string)null;

            if (!string.IsNullOrEmpty(_testPdf))
            {
                path = _testPdf;
            }
            else if (args.Length > 0)
            {
                path = args[0];
            }

            if (!string.IsNullOrEmpty(path))
            {
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.pdf"); //Search pattern is case insensitive
                    if (files.Length > 0)
                    {
                        return Pdf.List(files.ToArray());
                    }
                }
                else if (File.Exists(path) && path.EndsWithIgnoreCase(".pdf"))
                {
                    return Pdf.List(path);
                }
            }

            //Nothing to do
            return Pdf.List();
        }
    }
}
