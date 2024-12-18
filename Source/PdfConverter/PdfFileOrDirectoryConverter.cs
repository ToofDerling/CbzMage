using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Settings;
using PdfConverter.Exceptions;
using PdfConverter.Helpers;
using System.Diagnostics;

namespace PdfConverter
{
    public class PdfFileOrDirectoryConverter
    {
        private int _pagesCount = 0;

        public async Task ConvertFileOrDirectoryAsync(string path)
        {
            var config = new PdfConvertSettings();
            config.CreateSettings();

            if (!string.IsNullOrWhiteSpace(Settings.CbzDir))
            {
                ProgressReporter.Info($"Cbz backups: {Settings.CbzDir}");
            }
            ProgressReporter.Info($"Jpq quality: {Settings.JpgQuality}");
            ProgressReporter.Info($"Cbz compression: {Settings.CompressionLevel}");
            ProgressReporter.Info($"Conversion threads: {Settings.NumberOfThreads}");

#if DEBUG
            ProgressReporter.Info($"{nameof(Settings.ReadRequestSize)}: {Settings.ReadRequestSize}");
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
            foreach(var pdf in  pdfList) 
            {
                await ConvertPdfAsync(pdf, converter);
            }

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed;
            var secsPerPage = elapsed.TotalSeconds / _pagesCount;

            ProgressReporter.Info($"{_pagesCount} pages converted in {elapsed.Hhmmss()} ({secsPerPage:F2} sec/page)");

            DebugStatsCount.ShowStats();
        }

        private async Task ConvertPdfAsync(Pdf pdf, ConverterEngine converter)
        {
            var stopwatch = Stopwatch.StartNew();

            ProgressReporter.Info(pdf.PdfPath);

            try
            {
                using var pdfParser = new PdfParser(pdf);

                ProgressReporter.Info($"{pdf.PageCount} pages");
                _pagesCount += pdf.PageCount;

                await converter.ConvertToCbzAsync(pdf, pdfParser);
            }
            catch (PdfEncryptedException)
            {
                ProgressReporter.Error($"Error reading [{pdf.PdfPath}] pdf is encrypted");
            }

            stopwatch.Stop();
            ProgressReporter.Info($"{stopwatch.Elapsed.Mmss()}");

            ProgressReporter.Line();
        }

        private static List<Pdf> InitializePdfPath(string path)
        {
            SearchOption searchOption;

            if (string.IsNullOrEmpty(path))
            {
                path = Environment.CurrentDirectory;
                searchOption = SearchOption.TopDirectoryOnly;
            }
            else
            {
                // Must run before before the checks for file/dir existance
                path = SharedSettings.GetDirectorySearchOption(path, out searchOption);
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

            // Nothing to do
            return Pdf.List();
        }
    }
}
