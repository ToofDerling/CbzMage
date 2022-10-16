using ImageMagick;
using System.Collections.Concurrent;

namespace PdfConverter.Ghostscript
{
    public class GetSinglePipedImageDataHandler : IPipedImageDataHandler
    {
        private readonly BlockingCollection<MagickImage> _queue = new();

        public MagickImage WaitForImageDate()
        {
            var buffer = _queue.Take();

            _queue.Dispose();

            return buffer;
        }

        public void HandleImageData(MagickImage image)
        {
            if (image == null)
            {
                return;
            }

            _queue.Add(image);
        }
    }
}
