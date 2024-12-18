using CbzMage.Shared.Extensions;
using CbzMage.Shared.JobQueue;
using PdfConverter.Exceptions;
using PdfConverter.ImageData;
using PdfConverter.Jobs;
using PdfConverter.PageInfo;
using System.Collections.Concurrent;

namespace PdfConverter
{
    public class PageConverter : IImageDataHandler
    {
        private readonly JobExecutor<AbstractPdfPageInfo> _converterExecutor;
        private readonly JobWaiter _jobWaiter;

        private readonly ConcurrentDictionary<int, AbstractPdfPageInfo> _convertedPages;

        public PageConverter(Queue<int> pageQueue, ConcurrentDictionary<int, AbstractPdfPageInfo> convertedPages)
        {
            _convertedPages = convertedPages;

            _converterExecutor = new JobExecutor<AbstractPdfPageInfo>();
            _converterExecutor.JobExecuted += (s, e) => OnImageConverted(e);

            _jobWaiter = _converterExecutor.Start(withWaiter: true);
        }

        public void WaitForPagesConverted() => _jobWaiter.WaitForJobsToFinish();

        public void HandleImageData(AbstractPdfPageInfo pageInfo)
        {
            if (pageInfo == null)
            {
                _converterExecutor.Stop();
                return;
            }

            var job = new ImageConverterJob(pageInfo);
            _converterExecutor.AddJob(job);
        }

        private void OnImageConverted(JobEventArgs<AbstractPdfPageInfo> eventArgs)
        {
            var pageInfo = eventArgs.Result;

            if (!_convertedPages.TryAdd(pageInfo.PageNumber, pageInfo))
            {
                throw new SomethingWentWrongSorryException($"{pageInfo.PageNumber.ToPageString()} already converted?");
            }

            PageConverted?.Invoke(this, new PageConvertedEventArgs(pageInfo.PageNumber));
        }

        public event EventHandler<PageConvertedEventArgs> PageConverted;
    }
}
