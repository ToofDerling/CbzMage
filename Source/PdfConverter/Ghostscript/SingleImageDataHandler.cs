using CbzMage.Shared.Buffers;
using System.Collections.Concurrent;

namespace PdfConverter.Ghostscript
{
    public class SingleImageDataHandler : IImageDataHandler
    {
        private readonly BlockingCollection<ByteArrayBufferWriter> _queue = new();

        public ByteArrayBufferWriter WaitForImageDate()
        {
            var buffer = _queue.Take();

            _queue.Dispose();

            return buffer;
        }

        public void HandleImageData(ByteArrayBufferWriter image)
        {
            if (image == null)
            {
                return;
            }

            _queue.Add(image);
        }
    }
}
