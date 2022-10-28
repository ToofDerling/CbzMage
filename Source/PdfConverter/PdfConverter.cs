using CbzMage.Shared.Extensions;
using PdfConverter.Ghostscript;
using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System.Diagnostics;

namespace PdfConverter
{
    public class PdfConverter
    {
        private int pagesCount = 0;

        public void ConvertFileOrDirectory(string path)
        {
            var config = new PdfSettings();
            config.CreateSettings();

            var pdfList = InitializePdfPath(path);
            if (!pdfList.Any())
            {
                Console.WriteLine("No pdf files found");
                return;
            }

            var gsVersion = GhostscriptPageMachineManager.GetGhostscriptVersion();
            if (gsVersion == null)
            {
                return;
            }

            Console.WriteLine($"Conversion mode: {Settings.PdfConverterMode}");
            Console.WriteLine($"Ghostscript version: {gsVersion.Version}");
            Console.WriteLine($"Ghostscript reader threads: {Settings.GhostscriptReaderThreads}");
            Console.WriteLine($"Jpq quality: {Settings.JpgQuality}");
#if DEBUG
            Console.WriteLine($"Running in DEBUG mode");
#endif
            Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();

            using (var pageMachineManager = new GhostscriptPageMachineManager(gsVersion))
            {
                using var bufferCache = new BufferCache(Settings.BufferSize);

                var converter = new ConverterEngine(pageMachineManager);
                pdfList.ForEach(pdf => ConvertPdf(pdf, converter));
            }

#if DEBUG
            StatsCount.ShowStats();
            Console.WriteLine();
#endif

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed;
            var secsPerPage = elapsed.TotalSeconds / pagesCount;

            Console.WriteLine($"{pagesCount} pages converted in {elapsed.Hhmmss()} ({secsPerPage:F2} sec/page)");
        }

        private void ConvertPdf(Pdf pdf, ConverterEngine converter)
        {
            var stopwatch = Stopwatch.StartNew();

            using var pdfParser = new PdfImageParser(pdf);

            Console.WriteLine(pdf.Path);
            Console.WriteLine($"{pdf.PageCount} pages");

            pagesCount += pdf.PageCount;

            converter.ConvertToCbz(pdf, pdfParser);

            stopwatch.Stop();

            Console.WriteLine($"{stopwatch.Elapsed.Mmss()}");
            Console.WriteLine();
        }

        private List<Pdf> InitializePdfPath(string path)
        {
            path ??= Environment.CurrentDirectory;

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.pdf");
                if (files.Length > 0)
                {
                    return Pdf.List(files.ToArray());
                }
            }
            else if (File.Exists(path) && path.EndsWithIgnoreCase(".pdf"))
            {
                return Pdf.List(path);
            }

            //Nothing to do
            return Pdf.List();
        }
    }
}