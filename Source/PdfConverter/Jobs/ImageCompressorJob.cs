using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System.Diagnostics;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImageCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly ZipArchive _compressor;

        private readonly List<(string page, ManagedMemoryStream image)> _imageList;

        private readonly ProgressReporter _progressReporter;

        public ImageCompressorJob(ZipArchive compressor, List<(string, ManagedMemoryStream)> imageList,
            ProgressReporter progressReporter)
        {
            _compressor = compressor;

            _imageList = imageList;

            _progressReporter = progressReporter;
        }

        public IEnumerable<string> Execute()
        {

#if DEBUG
            var stopwatch = new Stopwatch();
#endif

            foreach (var (page, jpgStream) in _imageList)
            {

#if DEBUG
                stopwatch.Restart();
#endif

                var entry = _compressor.CreateEntry(page);
                using var cbzStream = entry.Open();

                var buffer = jpgStream.GetBuffer();
                int written = (int)jpgStream.Length;

                cbzStream.Write(buffer, 0, written);

                jpgStream.Release();

#if DEBUG 
                stopwatch.Stop();
                StatsCount.AddMagickWrite((int)stopwatch.ElapsedMilliseconds, written);
#endif

                _progressReporter.ShowProgress($"Converted {page}");
            }

            return _imageList.Select(x => x.page);
        }
    }
}
