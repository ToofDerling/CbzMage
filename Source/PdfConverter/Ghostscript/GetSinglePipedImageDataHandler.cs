using PdfConverter.ManagedBuffers;
using System.Collections.Concurrent;

namespace PdfConverter.Ghostscript
{
    public class GetSinglePipedImageDataHandler : IPipedImageDataHandler
    {
        private readonly BlockingCollection<ManagedBuffer> _queue = new BlockingCollection<ManagedBuffer>();

        public ManagedBuffer WaitForImageDate()
        {
            var buffer = _queue.Take();

            _queue.Dispose();

            return buffer;
        }

        public void HandleImageData(ManagedBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            _queue.Add(buffer);
        }
    }
}
