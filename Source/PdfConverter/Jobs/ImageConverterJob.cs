using CbzMage.Shared.Buffers;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.JobQueue;
using ImageMagick;
using PdfConverter.Helpers;
using System.Diagnostics;

namespace PdfConverter.Jobs
{
    public class ImageConverterJob : IJobConsumer<(int pageNumber, ArrayPoolBufferWriter<byte> imageData, string imageExt)>
    {
        private readonly int _pageNumber;

        private readonly ArrayPoolBufferWriter<byte> _bufferWriter;

        private readonly string _imageExt;

        private readonly int? _resizeHeight;

        public string SaveDir { get; set; }

        public ImageConverterJob(int pageNumber, ArrayPoolBufferWriter<byte> bufferWriter, string imageExt, int? resizeHeight)
        {
            _pageNumber = pageNumber;

            _bufferWriter = bufferWriter;

            _imageExt = imageExt;

            _resizeHeight = resizeHeight;
        }

        public Task<(int pageNumber, ArrayPoolBufferWriter<byte> imageData, string imageExt)> ConsumeAsync()
        {

#if DEBUG 
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var pngSize = _bufferWriter.WrittenCount;
            using var image = new MagickImage(_bufferWriter.WrittenSpan);

            /* itext code:
                 return IdentifyImageType() switch
                 {
                     ImageType.PNG => "png",
                     ImageType.JPEG => "jpg",
                     ImageType.JPEG2000 => "jp2",
                     ImageType.TIFF => "tif",
                     ImageType.JBIG2 => "jbig2",
                     _ => throw new InvalidOperationException("Should have never happened. This type of image is not allowed for ImageXObject"),
                 };
            */
            switch (_imageExt)
            {
                case "png":
                    image.Format = MagickFormat.Png;
                    image.Quality = 100;
                    break;
                default:
                    // Produce baseline jpgs with no subsampling.
                    image.Format = MagickFormat.Jpg;
                    image.Quality = Settings.JpgQuality;
                    break;
            }

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

            // Reuse buffer for the converted image 
            _bufferWriter.Reset();
            image.Write(_bufferWriter);

            var jpgSize = _bufferWriter.WrittenCount;

            if (!string.IsNullOrEmpty(SaveDir))
            {
                var page = _pageNumber.ToPageString(_imageExt);
                var pageFile = Path.Combine(SaveDir, page);

                File.WriteAllBytes(pageFile, _bufferWriter.WrittenSpan.ToArray());
            }

#if DEBUG 
            stopwatch.Stop();
            StatsCount.AddImageConversion((int)stopwatch.ElapsedMilliseconds, resized, pngSize, jpgSize);
#endif
            return Task.FromResult((_pageNumber, _bufferWriter, _imageExt));
        }
    }
}
