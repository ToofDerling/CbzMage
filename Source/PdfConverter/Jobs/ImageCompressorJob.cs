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

        private readonly List<(string page, MagickImage image)> _imageList;

        private readonly ProgressReporter _progressReporter;

        public ImageCompressorJob(ZipArchive compressor, List<(string, MagickImage)> imageList,
            ProgressReporter progressReporter)
        {
            _compressor = compressor;

            _imageList = imageList;

            _progressReporter = progressReporter;
        }

        public IEnumerable<string> Execute()
        {
            var stopwatch = new Stopwatch();

            foreach (var (page, image) in _imageList)
            {
                stopwatch.Restart();

                var entry = _compressor.CreateEntry(page, CompressionLevel.Fastest);
                using var archiveStream = entry.Open();

                image.Write(archiveStream);
                image.Dispose();

                stopwatch.Stop();
                StatsCount.AddMagickWrite((int)stopwatch.ElapsedMilliseconds, (int)archiveStream.Position);

                _progressReporter.ShowProgress($"Converted {page}");
            }

            return _imageList.Select(x => x.page);
        }
    }
}
