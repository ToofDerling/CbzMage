using CbzMage.Shared.Buffers;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using PdfConverter.Jobs;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace PdfConverter
{
    public class PageCompressor
    {
        private readonly Pdf _pdf;

        private readonly ConcurrentQueue<int> _pageNumbers;

        private readonly ConcurrentDictionary<string, ByteArrayBufferWriter> _convertedPages;

        private readonly JobExecutor<IEnumerable<string>> _compressorExecutor;

        private readonly JobWaiter _jobWaiter;

        private readonly ZipArchive _compressor;

        private bool _addedJob = false;

        private int _nextPageNumber;

        private readonly ProgressReporter _progressReporter;

        public PageCompressor(Pdf pdf, ConcurrentDictionary<string, ByteArrayBufferWriter> convertedPages)
        {
            _pdf = pdf;
            _convertedPages = convertedPages;

            _pageNumbers = new ConcurrentQueue<int>(Enumerable.Range(1, pdf.PageCount));
            _pageNumbers.TryDequeue(out _nextPageNumber);

            _compressorExecutor = new JobExecutor<IEnumerable<string>>(ThreadPriority.Highest);
            _compressorExecutor.JobExecuted += (s, e) => OnImagesCompressed(e);

            _jobWaiter = _compressorExecutor.Start(withWaiter: true);

            _compressor = CreateCompressor();
            _progressReporter = new ProgressReporter(pdf.PageCount);
        }

        private ZipArchive CreateCompressor()
        {
            var cbzFile = Path.ChangeExtension(_pdf.Path, ".cbz");
            File.Delete(cbzFile);

            ProgressReporter.Done(cbzFile);

            return ZipFile.Open(cbzFile, ZipArchiveMode.Create);
        }

        public void WaitForPagesCompressed()
        {
            _jobWaiter.WaitForJobsToFinish();
            _compressor.Dispose();
        }

        public void OnPageConverted(PageConvertedEventArgs _)
        {
            if (!_addedJob)
            {
                _addedJob = AddCompressorJob();
            }
        }

        public void SignalAllPagesConverted()
        {
            AddCompressorJob();

            _compressorExecutor.Stop();
        }

        private void OnImagesCompressed(JobEventArgs<IEnumerable<string>> eventArgs)
        {
            PagesCompressed?.Invoke(this, new PagesCompressedEventArgs(eventArgs.Result));

            _addedJob = AddCompressorJob();
        }

        public event EventHandler<PagesCompressedEventArgs> PagesCompressed;

        private bool AddCompressorJob()
        {
            var key = _pdf.GetPageString(_nextPageNumber);

            var imageList = new List<(string page, ByteArrayBufferWriter imageData)>();

            while (_convertedPages.TryRemove(key, out var imageData))
            {
                imageList.Add((key, imageData));

                _pageNumbers.TryDequeue(out _nextPageNumber);

                key = _pdf.GetPageString(_nextPageNumber);
            }

            if (imageList.Count > 0)
            {
                var job = new ImageCompressorJob(_compressor, imageList, _progressReporter);
                _compressorExecutor.AddJob(job);

                return true;
            }
            return false;
        }
    }
}
