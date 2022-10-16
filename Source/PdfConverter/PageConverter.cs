using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.Ghostscript;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class PageConverter : IPipedImageDataHandler
    {
        private readonly Pdf _pdf;
        private readonly Queue<int> _pageQueue;
        private readonly ConcurrentDictionary<string, MagickImage> _convertedPages;

        public PageConverter(Pdf pdf, Queue<int> pageQueue, ConcurrentDictionary<string, MagickImage> convertedPages)
        {
            _pdf = pdf;
            _pageQueue = pageQueue;

            _convertedPages = convertedPages;
        }

        public void HandleImageData(MagickImage image)
        {
            if (image == null)
            {
                return;
            }

            var pageNumber = _pageQueue.Dequeue();
            var page = _pdf.GetPageString(pageNumber);

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

            if (!_convertedPages.TryAdd(page, image))
            {
                throw new SomethingWentWrongException($"{page} already converted?");
            }

            PageConverted?.Invoke(this, new PageConvertedEventArgs(page));
        }

        public event EventHandler<PageConvertedEventArgs> PageConverted;
    }
}
