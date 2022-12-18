using CbzMage.Shared.Jobs;
using CbzMage.Shared.ManagedBuffers;
using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PdfConverter.Jobs
{
    public class ImageConverterJob : IJob<string>
    {
        private readonly ConcurrentDictionary<string, ManagedMemoryStream> _convertedImages;

        private readonly ManagedBuffer _buffer;

        private readonly string _page;

        private readonly int? _resizeHeight;

        public ImageConverterJob(ManagedBuffer buffer, ConcurrentDictionary<string, ManagedMemoryStream> convertedImages,
            string page, int? resizeHeight)
        {
            _buffer = buffer;

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

            var pngSize = _buffer.Count;
            using var image = new MagickImage(_buffer.Buffer, 0, _buffer.Count);

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

            // Reuse the png buffer for the jpg stream. 
            var stream = new ManagedMemoryStream(_buffer.Buffer);

            bool largerBufferNeeded = false;
            try
            {
                image.Write(stream);
            }
            catch (MagickErrorException)
            {
                // This can happen when the conversion ends up with a jpg larger than the png.
                // So try with a regular resizable memorystream before giving up
                stream.Release();
                stream = new ManagedMemoryStream(pngSize * 2);

                image.Write(stream);
                largerBufferNeeded = true;
            }

            var jpgSize = (int)stream.Length;

#if DEBUG 
            stopwatch.Stop();
            StatsCount.AddImageConversion((int)stopwatch.ElapsedMilliseconds, 
                resized, pngSize, jpgSize, largerBufferNeeded);
#endif

            if (!_convertedImages.TryAdd(_page, stream))
            {
                throw new SomethingWentWrongSorryException($"{_page} already converted?");
            }
            return _page;
        }
    }
}
