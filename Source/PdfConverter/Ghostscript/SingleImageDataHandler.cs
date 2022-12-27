using CbzMage.Shared.Buffers;
using System.Collections.Concurrent;

namespace PdfConverter.Ghostscript
{
    public class SingleImageDataHandler : IImageDataHandler
    {
        private readonly BlockingCollection<ArrayPoolBufferWriter<byte>> _queue = new();

        public ArrayPoolBufferWriter<byte> WaitForImageDate()
        {
            var buffer = _queue.Take();

            _queue.Dispose();

            return buffer;
        }

        public void HandleImageData(ArrayPoolBufferWriter<byte> image)
        {
            if (image == null)
            {
                return;
            }

            _queue.Add(image);
        }
    }
}
