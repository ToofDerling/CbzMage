using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using PdfConverter.Jobs;
using PdfConverter.ManagedBuffers;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace PdfConverter
{
    public class PageCompressor
    {
        private readonly Pdf _pdf;

        private readonly ConcurrentQueue<int> _pageNumbers;

        private readonly ConcurrentDictionary<string, object> _convertedPages;

        private readonly JobExecutor<IEnumerable<string>> _compressorExecutor;

        private readonly JobWaiter _jobWaiter;

        private readonly ZipArchive _compressor;

        private bool _addedJob = false;

        private int _nextPageNumber;

        private readonly ProgressReporter _progressReporter;

        private readonly int? _resizeHeight;

        public PageCompressor(Pdf pdf, ConcurrentDictionary<string, object> convertedPages,
            int? resizeHeight)
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

            _resizeHeight = resizeHeight;
        }

        private ZipArchive CreateCompressor()
        {
            var _cbzFile = Path.ChangeExtension(_pdf.Path, ".cbz");
            File.Delete(_cbzFile);

            ProgressReporter.Done(_cbzFile);

            return ZipFile.Open(_cbzFile, ZipArchiveMode.Create);
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

            var inputList = new List<(string page, object imageData)>();

            while (_convertedPages.TryRemove(key, out var imageData))
            {
                inputList.Add((key, imageData));

                _pageNumbers.TryDequeue(out _nextPageNumber);

                key = _pdf.GetPageString(_nextPageNumber);
            }

            if (inputList.Count > 0)
            {
                IJob<IEnumerable<string>> job;

                if (inputList.First().imageData is ManagedMemoryStream)
                {
                    var imageList = inputList.Select(i => (i.page, i.imageData as ManagedMemoryStream)).AsList();

                    job = new ImageCompressorJob(_compressor, imageList, _progressReporter);
                }
                else
                {
                    var imagePathList = inputList.Select(i => (i.page, i.imageData.ToString())).AsList();

                    job = new ImageConverterAndCompressorJob(imagePathList, _resizeHeight,
                        _compressor, _progressReporter);
                }

                _compressorExecutor.AddJob(job);
                return true;
            }
            return false;
        }
    }
}
