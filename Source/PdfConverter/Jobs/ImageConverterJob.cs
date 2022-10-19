using CbzMage.Shared.Jobs;
using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PdfConverter.Jobs
{
    public class ImageConverterJob : IJob<string>
    {
        private readonly ConcurrentDictionary<string, object> _convertedImages;

        private readonly ManagedBuffer _buffer;

        private readonly string _page;

        private readonly int? _resizeHeight;

        public ImageConverterJob(ManagedBuffer buffer, 
            ConcurrentDictionary<string, object> convertedImages, 
            string page, int? resizeHeight)
        {
            _buffer = buffer;

            _convertedImages = convertedImages;

            _page = page;

            _resizeHeight = resizeHeight;   
        }

        public string Execute()
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var image = new MagickImage(_buffer.Buffer, 0, _buffer.Count);

            var resized = ImageHelper.ConvertJpg(image, _resizeHeight);
            stopwatch.Stop();

            StatsCount.AddMagickRead((int)stopwatch.ElapsedMilliseconds, resized, _buffer.Count);

            _buffer.Release();

            if (!_convertedImages.TryAdd(_page, image))
            {
                throw new SomethingWentWrongSorryException($"{_page} already converted?");
            }
            return _page;
        }
    }
}
