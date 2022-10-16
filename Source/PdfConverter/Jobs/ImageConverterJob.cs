using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.ManagedBuffers;
using System.Collections.Concurrent;

namespace PdfConverter.Jobs
{
    public class ImageConverterJob : IJob<string>
    {
        private readonly ConcurrentDictionary<string, MagickImage> _convertedImages;

        private readonly ManagedBuffer _buffer;

        private readonly string _page;

        public ImageConverterJob(ManagedBuffer buffer, ConcurrentDictionary<string, MagickImage> convertedImages, string page)
        {
            _buffer = buffer;

            _convertedImages = convertedImages;

            _page = page;
        }

        public string Execute()
        {
            var image = new MagickImage(_buffer.Buffer, 0, _buffer.Count)
            {
                Format = MagickFormat.Jpg,
                Interlace = Interlace.Plane,
                Quality = Program.QualityConstants.JpegQuality
            };

            if (image.Height > Program.QualityConstants.MaxHeightThreshold)
            {
                image.Resize(new MagickGeometry
                {
                    Greater = true,
                    Less = false,
                    Height = Program.QualityConstants.MaxHeight
                });
            }

            _buffer.Release();

            if (!_convertedImages.TryAdd(_page, image))
            {
                throw new SomethingWentWrongException($"{_page} already converted?");
            }
            return _page;
        }
    }
}
