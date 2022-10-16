using ImageMagick;
using PdfConverter.Helpers;
using System.Diagnostics;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImagesCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly ZipArchive _compressor;

        private readonly IDictionary<string, MagickImage> _inputMap;

        public ImagesCompressorJob(ZipArchive compressor, IDictionary<string, MagickImage> inputMap)
        {
            _compressor = compressor;

            _inputMap = inputMap;
        }

        public IEnumerable<string> Execute()
        {
            var stopwatch = new Stopwatch();

            foreach (var page in _inputMap)
            {
                var entry = _compressor.CreateEntry(page.Key, CompressionLevel.Fastest);
                using var archiveStream = entry.Open();
                
                var image = page.Value;

                var stream = new MemoryStream();
                stopwatch.Restart();
                image.Write(stream);
                stopwatch.Stop();

                StatsCount.AddMagicWriteTime(stopwatch.ElapsedMilliseconds);

                stream.CopyTo(archiveStream);

                //image.Write(archiveStream);
                image.Dispose();

                StatsCount.AddJpg((int)archiveStream.Position);
            }

            return _inputMap.Keys;
        }
    }
}
