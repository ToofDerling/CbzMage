using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Settings;
using PdfConverter.Helpers;
using System.Diagnostics;

namespace PdfConverter
{
    public class PdfFileOrDirectoryConverter
    {
        private int _pagesCount = 0;

        public void ConvertFileOrDirectory(string path)
        {
            var config = new PdfConvertSettings();
            config.CreateSettings();

            if (!string.IsNullOrWhiteSpace(Settings.CbzDir))
            {
                ProgressReporter.Info($"Cbz backups: {Settings.CbzDir}");
            }
            if (Settings.GhostscriptVersion != null)
            {
                ProgressReporter.Info($"Ghostscript version: {Settings.GhostscriptVersion}");
            }
            ProgressReporter.Info($"Ghostscript reader threads: {Settings.NumberOfThreads}");
            ProgressReporter.Info($"Jpq quality: {Settings.JpgQuality}");
            ProgressReporter.Info($"Cbz compression: {Settings.CompressionLevel}");

#if DEBUG
            ProgressReporter.Info($"{nameof(Settings.WriteBufferSize)}: {Settings.WriteBufferSize}");
            ProgressReporter.Info($"{nameof(Settings.ImageBufferSize)}: {Settings.ImageBufferSize}");
#endif

            ProgressReporter.Line();

            var pdfList = InitializePdfPath(path);
            if (!pdfList.Any())
            {
                ProgressReporter.Error("No pdf files found");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            var converter = new ConverterEngine();
            pdfList.ForEach(pdf => ConvertPdf(pdf, converter));

#if DEBUG
            StatsCount.ShowStats();
            ProgressReporter.Line();
#endif

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed;
            var secsPerPage = elapsed.TotalSeconds / _pagesCount;

            ProgressReporter.Info($"{_pagesCount} pages converted in {elapsed.Hhmmss()} ({secsPerPage:F2} sec/page)");
        }

        private void ConvertPdf(Pdf pdf, ConverterEngine converter)
        {
            var stopwatch = Stopwatch.StartNew();

            // Throws if pdf is encrypted
            using var pdfParser = new PdfImageParser(pdf);

            ProgressReporter.Info(pdf.Path);
            ProgressReporter.Info($"{pdf.PageCount} pages");

            _pagesCount += pdf.PageCount;

            converter.ConvertToCbz(pdf, pdfParser);

            stopwatch.Stop();

            ProgressReporter.Info($"{stopwatch.Elapsed.Mmss()}");
            ProgressReporter.Line();
        }

        private List<Pdf> InitializePdfPath(string path)
        {
            // Must run before before the checks for file/dir existance
            path = SharedSettings.GetDirectorySearchOption(path, out var searchOption);

            if (string.IsNullOrEmpty(path))
            {
                path = Environment.CurrentDirectory;
            }

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.pdf", searchOption);

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
