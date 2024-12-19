using CbzMage.Shared.Buffers;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.JobQueue;
using PdfConverter.Exceptions;
using PdfConverter.Jobs;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class PageConverter : IImageDataHandler
    {
        private readonly JobExecutor<(int pageNumber, ArrayPoolBufferWriter<byte> imageData, string imageExt)> _converterExecutor;
        private readonly JobWaiter _jobWaiter;

        private readonly Queue<int> _pageQueue;
        private readonly ConcurrentDictionary<int, (ArrayPoolBufferWriter<byte> imageData, string imageExt)> _convertedPages;

        private readonly int? _resizeHeight;

        public PageConverter(Queue<int> pageQueue, ConcurrentDictionary<int, (ArrayPoolBufferWriter<byte> imageData, string imageExt)> convertedPages, int? resizeHeight)
        {
            _pageQueue = pageQueue;

            _convertedPages = convertedPages;

            _resizeHeight = resizeHeight;

            _converterExecutor = new JobExecutor<(int pageNumber, ArrayPoolBufferWriter<byte> imageData, string imageExt)>();
            _converterExecutor.JobExecuted += (s, e) => OnImageConverted(e);

            _jobWaiter = _converterExecutor.Start(withWaiter: true);
        }

        public void WaitForPagesConverted() => _jobWaiter.WaitForJobsToFinish();

        // Handle png/jpg (possible other types) imagedata from Itext saving original pdf images.
        public void HandleSavedImageData(ArrayPoolBufferWriter<byte> bufferWriter, string imageExt)
        {
            if (bufferWriter == null)
            {
                _converterExecutor.Stop();
                return;
            }

            var pageNumber = _pageQueue.Dequeue();

            // It makes no sense to convert jpg images
            if (imageExt == ImageExt.Jpg)
            {
                OnImageConverted(new JobEventArgs<(int pageNumber, ArrayPoolBufferWriter<byte> imageData, string imageExt)>((pageNumber, bufferWriter, imageExt)));
                return;
            }

            // But it does makes sense to recompress png images as much as possible.
            // Converter logic is png -> recompress, everything else -> convert to jpg.
            var job = new ImageConverterJob(pageNumber, bufferWriter, imageExt, null);
            _converterExecutor.AddJob(job);
        }

        // Handle png imagedata from Ghostscript rendering a pdf. 
        public void HandleRenderedImageData(ArrayPoolBufferWriter<byte> bufferWriter)
        {
            if (bufferWriter == null)
            {
                _converterExecutor.Stop();
                return;
            }

            var pageNumber = _pageQueue.Dequeue();

            // Tell converter to convert to jpg and resize if needed.
            var job = new ImageConverterJob(pageNumber, bufferWriter, ImageExt.Jpg, _resizeHeight);
            _converterExecutor.AddJob(job);
        }

        private void OnImageConverted(JobEventArgs<(int pageNumber, ArrayPoolBufferWriter<byte> imageData, string imageExt)> eventArgs)
        {
            var (pageNumber, imageData, imageExt) = eventArgs.Result;

            if (!_convertedPages.TryAdd(pageNumber, (imageData, imageExt)))
            {
                throw new SomethingWentWrongSorryException($"{pageNumber.ToPageString()} already converted?");
            }

            PageConverted?.Invoke(this, new PageConvertedEventArgs(pageNumber));
        }

        public event EventHandler<PageConvertedEventArgs> PageConverted;
    }
}
