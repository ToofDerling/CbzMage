using PdfConverter.PageInfo;
using System.Collections.Concurrent;

namespace PdfConverter.ImageData
{
    public class SingleImageDataHandler : IImageDataHandler
    {
        private readonly BlockingCollection<AbstractPdfPageInfo> _queue = new();

        public AbstractPdfPageInfo WaitForImageDate()
        {
            var buffer = _queue.Take();

            _queue.Dispose();

            return buffer;
        }

        public void HandleImageData(AbstractPdfPageInfo image)
        {
            if (image == null)
            {
                return;
            }

            _queue.Add(image);
        }
    }
}
