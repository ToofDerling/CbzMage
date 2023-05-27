using CbzMage.Shared.Buffers;
using CbzMage.Shared.Jobs;
using CbzMage.Shared.Settings;
using PdfConverter.Ghostscript;
using PdfConverter.Jobs;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class PageConverter : IImageDataHandler
    {
        private readonly JobExecutor<string> _converterExecutor;
        private readonly JobWaiter _jobWaiter;

        private readonly Queue<int> _pageQueue;
        private readonly ConcurrentDictionary<string, ArrayPoolBufferWriter<byte>> _convertedPages;

        private readonly int? _resizeHeight;

        public PageConverter(Queue<int> pageQueue,  
            ConcurrentDictionary<string, ArrayPoolBufferWriter<byte>> convertedPages, int? resizeHeight)
        {
            _pageQueue = pageQueue;

            _convertedPages = convertedPages;

            _converterExecutor = new JobExecutor<string>(ThreadPriority.AboveNormal);
            _converterExecutor.JobExecuted += (s, e) => OnImageConverted(e);
            
            _jobWaiter = _converterExecutor.Start(withWaiter: true);

            _resizeHeight = resizeHeight;
        }

        public void WaitForPagesConverted()
        {
            _jobWaiter.WaitForJobsToFinish();
        }

        public void HandleImageData(ArrayPoolBufferWriter<byte> bufferWriter)
        {
            if (bufferWriter == null)
            {
                _converterExecutor.Stop();
                return;
            }
            var pageNumber = _pageQueue.Dequeue();
            var page = SharedSettings.GetPageString(pageNumber);

            var job = new ImageConverterJob(bufferWriter , _convertedPages, page, _resizeHeight);
            _converterExecutor.AddJob(job);
        }

        private void OnImageConverted(JobEventArgs<string> eventArgs)
        {
            PageConverted?.Invoke(this, new PageConvertedEventArgs(eventArgs.Result));
        }
              
        public event EventHandler<PageConvertedEventArgs> PageConverted;
    }
}
