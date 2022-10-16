using CbzMage.Shared.Jobs;
using ImageMagick;
using PdfConverter.Exceptions;
using PdfConverter.Ghostscript;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class PageConverter : JobWaiter, IPipedImageDataHandler
    {
        private readonly Pdf _pdf;
        private readonly Queue<int> _pageQueue;

        private readonly ConcurrentDictionary<string, MagickImage> _convertedPages;
        private readonly int _wantedHeight;

        public PageConverter(Pdf pdf, Queue<int> pageQueue, 
            ConcurrentDictionary<string, MagickImage> convertedPages, int wantedHeight)
        {
            _pdf = pdf;
            _pageQueue = pageQueue;

            _convertedPages = convertedPages;
            _wantedHeight = wantedHeight;
        }

        public void WaitForPagesConverted()
        { 
            WaitForJobsToFinish();
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
            image.Quality = Program.Settings.JpegQuality;

            if (image.Height > _wantedHeight)
            {
                image.Resize(new MagickGeometry
                {
                    Greater = true,
                    Less = false,
                    Height = _wantedHeight
                });
            }

            if (!_convertedPages.TryAdd(page, image))
            {
                throw new SomethingWentWrongException($"{page} already converted?");
            }

            PageConverted?.Invoke(this, new PageConvertedEventArgs(page));

            if (_pageQueue.Count == 0)
            {
                _waitingQueue.Add("Done");
            }
        }

        public event EventHandler<PageConvertedEventArgs> PageConverted;
    }
}
