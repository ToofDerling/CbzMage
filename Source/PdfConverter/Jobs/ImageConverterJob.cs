using CbzMage.Shared.Buffers;
using CbzMage.Shared.Jobs;
using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PdfConverter.Jobs
{
    public class ImageConverterJob : IJob<string>
    {
        private readonly ConcurrentDictionary<string, ArrayPoolBufferWriter<byte>> _convertedImages;

        private readonly ArrayPoolBufferWriter<byte> _bufferWriter;

        private readonly string _page;

        private readonly int? _resizeHeight;

        public ImageConverterJob(ArrayPoolBufferWriter<byte> bufferWriter, 
            ConcurrentDictionary<string, ArrayPoolBufferWriter<byte>> convertedImages,
            string page, int? resizeHeight)
        {
            _bufferWriter = bufferWriter;

            _convertedImages = convertedImages;

            _page = page;

            _resizeHeight = resizeHeight;
        }

        public string Execute()
        {

#if DEBUG 
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var pngSize = _bufferWriter.WrittenCount;
            using var image = new MagickImage(_bufferWriter.WrittenSpan);

            // Produce baseline jpgs with no subsampling.
            image.Format = MagickFormat.Jpg;
            image.Quality = Settings.JpgQuality;

            var resized = false;
            if (_resizeHeight.HasValue && image.Height > _resizeHeight.Value)
            {
                resized = true;

                image.Resize(new MagickGeometry
                {
                    Greater = true,
                    Less = false,
                    Height = _resizeHeight.Value
                });
            }

            // Reuse the png buffer for the jpg. 
            _bufferWriter.Reset();
            image.Write(_bufferWriter);

            var jpgSize = _bufferWriter.WrittenCount;

#if DEBUG 
            stopwatch.Stop();
            StatsCount.AddImageConversion((int)stopwatch.ElapsedMilliseconds, resized, pngSize, jpgSize);
#endif

            if (!_convertedImages.TryAdd(_page, _bufferWriter))
            {
                throw new SomethingWentWrongSorryException($"{_page} already converted?");
            }
            return _page;
        }
    }
}
