using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Settings;
using System.Diagnostics;

namespace EpubConverter
{
    public class EpubFileOrDirectoryConverter
    {
        private int _pagesCount = 0;

        public async Task ConvertFileOrDirectoryAsync(string path)
        {
            //path = @"C:\System\epub\KonoSuba TRPG [Yen Press][Kobo]";
            //path = @"C:\System\epub\Bye Bye Birdie - Hughes, Shirley";
            path = @"C:\System\epub\isabella1";
            if (Directory.Exists(path)) 
            { 
                var epub = new Epub(path);
                var parser = new EpubParser(epub);
                await parser.ParseAsync();
                return;
            }


            var config = new EpubConvertSettings();
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

            var pdfList = InitializeEpubPath(path);
            if (!pdfList.Any())
            {
                ProgressReporter.Error("No epub files found");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            var converter = new ConverterEngine();
            pdfList.ForEach(pdf => ConvertPdf(pdf, converter));

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed;
            var secsPerPage = elapsed.TotalSeconds / _pagesCount;

            ProgressReporter.Info($"{_pagesCount} pages converted in {elapsed.Hhmmss()} ({secsPerPage:F2} sec/page)");
        }

        private void ConvertPdf(Epub epub, ConverterEngine converter)
        {
            var stopwatch = Stopwatch.StartNew();

            // Throws if pdf is encrypted
            var epubParser = new EpubParser(epub);

            ProgressReporter.Info(epub.Path);
            ProgressReporter.Info($"{epub.PageList.Count} pages");

            _pagesCount += epub.PageList.Count;

            converter.ConvertToCbz(epub, epubParser);

            stopwatch.Stop();

            ProgressReporter.Info($"{stopwatch.Elapsed.Mmss()}");
            ProgressReporter.Line();
        }

        private static List<Epub> InitializeEpubPath(string path)
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
                var files = Directory.GetFiles(path, "*.epub", searchOption);

                if (files.Length > 0)
                {
                    return Epub.List(files.ToArray());
                }
            }
            else if (File.Exists(path) && path.EndsWithIgnoreCase(".epub"))
            {
                return Epub.List(path);
            }

            //Nothing to do
            return Epub.List();
        }
    }
}
