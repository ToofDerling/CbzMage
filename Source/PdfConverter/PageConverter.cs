using PdfConverter.Ghostscript;
using PdfConverter.Jobs;
using PdfConverter.ManagedBuffers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace PdfConverter
{
    public class PageConverter : IPipedImageDataHandler
    {
        private readonly JobExecutor<string> _converterExecutor;
        private readonly JobWaiter _jobWaiter;

        private readonly Pdf _pdf;
        private readonly Queue<int> _pageQueue;
        private readonly ConcurrentDictionary<string, Stream> _convertedPages;

        public PageConverter(Pdf pdf, Queue<int> pageQueue, ConcurrentDictionary<string, Stream> convertedPages)
        {
            _pdf = pdf;
            _pageQueue = pageQueue;

            _convertedPages = convertedPages;

            _converterExecutor = new JobExecutor<string>();
            _converterExecutor.JobExecuted += (s, e) => OnImageConverted(e);

            _jobWaiter = _converterExecutor.Start(withWaiter: true);
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

            var job = new ImageConverterJob(buffer, _convertedPages, page);
            _converterExecutor.AddJob(job);
        }
        
        private void OnImageConverted(JobEventArgs<string> eventArgs)
        {
            PageConverted?.Invoke(this, new PageConvertedEventArgs(eventArgs.Result));
        }

        public event EventHandler<PageConvertedEventArgs> PageConverted;
    }
}
