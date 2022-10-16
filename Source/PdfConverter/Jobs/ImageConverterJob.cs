using ImageMagick;
using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System.Collections.Concurrent;
using System.IO;

namespace PdfConverter.Jobs
{
    public class ImageConverterJob : IJob<string>
    {
        private readonly ConcurrentDictionary<string, Stream> _convertedImages;

        private readonly ManagedBuffer _buffer;

        private readonly string _page;

        public ImageConverterJob(ManagedBuffer buffer, ConcurrentDictionary<string, Stream> convertedImages, string page)
        {
            _buffer = buffer;

            _convertedImages = convertedImages;

            _page = page;
        }

        public string Execute()
        {
            var buffer = ManagedMemoryStream.ManagedBuffer();
            var stream = new ManagedMemoryStream(buffer);

            using (var image = new MagickImage(_buffer.Buffer, 0, _buffer.Count))
            {
                image.Format = MagickFormat.Jpg;
                image.Interlace = Interlace.Plane;
                image.Quality = Program.QualityConstants.JpegQuality;

                if (image.Height > Program.QualityConstants.MaxHeightThreshold)
                {
                    image.Resize(new MagickGeometry
                    {
                        Greater = true,
                        Less = false,
                        Height = Program.QualityConstants.MaxHeight
                    });
                }

                image.Write(stream);
            };

            _buffer.Release();

            stream.Seek(0, SeekOrigin.Begin);
            StatsCount.AddJpg((int)stream.Length);

            if (!_convertedImages.TryAdd(_page, stream))
            {
                throw new SomethingWentWrongException($"{_page} already converted?");
            }
            return _page;
        }
    }
}
