using CbzMage.Shared.Jobs;
using ImageMagick;
using PdfConverter.Ghostscript;
using PdfConverter.Jobs;
using PdfConverter.ManagedBuffers;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class PageConverter : IPipedImageDataHandler
    {
        private readonly JobExecutor<string> _converterExecutor;
        private readonly JobWaiter _jobWaiter;

        private readonly Pdf _pdf;
        private readonly Queue<int> _pageQueue;
        private readonly ConcurrentDictionary<string, MagickImage> _convertedPages;

        private readonly int _wantedHeight;

        public PageConverter(Pdf pdf, Queue<int> pageQueue, ConcurrentDictionary<string, MagickImage> convertedPages, int wantedHeight)
        {
            _pdf = pdf;
            _pageQueue = pageQueue;

            _convertedPages = convertedPages;

            var threadCount = Math.Max(Settings.ThreadCount / 2, 1);

            _converterExecutor = new JobExecutor<string>(numThreads: threadCount);

            _converterExecutor.JobExecuted += (s, e) => OnImageConverted(e);
            
            _jobWaiter = _converterExecutor.Start(withWaiter: true);

            _wantedHeight = wantedHeight;
        }

        public void WaitForPagesConverted()
        {
            _jobWaiter.WaitForJobsToFinish();
        }

        public void HandleImageData(ManagedBuffer buffer)
        {
            if (buffer == null)
            {
                _converterExecutor.Stop();
                return;
            }

            var pageNumber = _pageQueue.Dequeue();
            var page = _pdf.GetPageString(pageNumber);

            var job = new ImageConverterJob(buffer, _convertedPages, page, _wantedHeight);
            _converterExecutor.AddJob(job);
        }

        private void OnImageConverted(JobEventArgs<string> eventArgs)
        {
            PageConverted?.Invoke(this, new PageConvertedEventArgs(eventArgs.Result));
        }

        public event EventHandler<PageConvertedEventArgs> PageConverted;
    }
}
