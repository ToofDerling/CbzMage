using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using ImageMagick;
using PdfConverter.Helpers;
using System.Diagnostics;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImageCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly ZipArchive _compressor;

        private readonly IDictionary<string, MagickImage> _inputMap;

        private readonly ProgressReporter _progressReporter;

        public ImageCompressorJob(ZipArchive compressor, IDictionary<string, MagickImage> inputMap,
            ProgressReporter progressReporter)
        {
            _compressor = compressor;

            _inputMap = inputMap;

            _progressReporter = progressReporter;
        }

        public IEnumerable<string> Execute()
        {
            var stopwatch = new Stopwatch();

            foreach (var page in _inputMap)
            {
                var entry = _compressor.CreateEntry(page.Key, CompressionLevel.Fastest);
                using var archiveStream = entry.Open();

                var image = page.Value;
                
                stopwatch.Restart();

                image.Write(archiveStream);
                image.Dispose();

                stopwatch.Stop();
                StatsCount.AddMagickWrite((int)stopwatch.ElapsedMilliseconds, (int)archiveStream.Position);

                _progressReporter.ShowProgress($"Converted {page.Key}");
            }

            return _inputMap.Keys;
        }
    }
}
