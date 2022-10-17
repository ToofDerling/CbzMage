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
        private readonly ConcurrentDictionary<string, MagickImage> _convertedImages;

        private readonly ManagedBuffer _buffer;

        private readonly string _page;

        private readonly int _wantedHeight;

        public ImageConverterJob(ManagedBuffer buffer, ConcurrentDictionary<string, MagickImage> convertedImages, string page, int wantedHeight)
        {
            _buffer = buffer;

            _convertedImages = convertedImages;

            _page = page;

            _wantedHeight = wantedHeight;   
        }

        public string Execute()
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var image = new MagickImage(_buffer.Buffer, 0, _buffer.Count)
            {
                Format = MagickFormat.Jpg,
                Interlace = Interlace.Plane,
                Quality = Program.Settings.JpegQuality
            };

            var resize = false;
            if (image.Height > _wantedHeight + Program.Settings.ResizeSlack)
            {
                resize = true;

                image.Resize(new MagickGeometry
                {
                    Greater = true,
                    Less = false,
                    Height = _wantedHeight
                });
            }

            stopwatch.Stop();

            StatsCount.AddMagickRead((int)stopwatch.ElapsedMilliseconds, resize, _buffer.Count);

            _buffer.Release();

            if (!_convertedImages.TryAdd(_page, image))
            {
                throw new SomethingWentWrongSorryException($"{_page} already converted?");
            }
            return _page;
        }
    }
}
