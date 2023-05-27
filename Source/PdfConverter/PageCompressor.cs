using CbzMage.Shared.Buffers;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using CbzMage.Shared.Settings;
using PdfConverter.Jobs;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace PdfConverter
{
    public class PageCompressor
    {
        private readonly Pdf _pdf;

        private readonly ConcurrentQueue<int> _pageNumbers;

        private readonly ConcurrentDictionary<string, ArrayPoolBufferWriter<byte>> _convertedPages;

        private readonly JobExecutor<IEnumerable<string>> _compressorExecutor;

        private readonly JobWaiter _jobWaiter;

        private readonly string _cbzFile;

        private readonly ZipArchive? _compressor;

        private readonly string? _coverFile;

        private bool _addedJob = false;

        private int _nextPageNumber;

        private readonly ProgressReporter _progressReporter;

        public PageCompressor(Pdf pdf, ConcurrentDictionary<string, ArrayPoolBufferWriter<byte>> convertedPages)
        {
            _pdf = pdf;
            _convertedPages = convertedPages;

            _pageNumbers = new ConcurrentQueue<int>(Enumerable.Range(1, pdf.PageCount));
            _pageNumbers.TryDequeue(out _nextPageNumber);

            _compressorExecutor = new JobExecutor<IEnumerable<string>>(ThreadPriority.Highest);
            _compressorExecutor.JobExecuted += (s, e) => OnImagesCompressed(e);

            _jobWaiter = _compressorExecutor.Start(withWaiter: true);

            _cbzFile = CreateCbzFile();
            _compressor = CreateCompressor();
            _coverFile = CreateCoverFile();

            _progressReporter = new ProgressReporter(pdf.PageCount);
        }

        private string CreateCbzFile()
        {
            var cbzFile = Path.ChangeExtension(_pdf.Path, ".cbz");

            if (!string.IsNullOrEmpty(Settings.CbzDir))
            {
                cbzFile = Path.Combine(Settings.CbzDir, Path.GetFileName(cbzFile));
            }

            return cbzFile;
        }

        private ZipArchive? CreateCompressor()
        {
            if (Settings.SaveCoverOnly)
            {
                return null;
            }

            File.Delete(_cbzFile);

            ProgressReporter.Done(_cbzFile);

            return ZipFile.Open(_cbzFile, ZipArchiveMode.Create);
        }

        private string? CreateCoverFile()
        {
            if (!Settings.SaveCover)
            {
                return null;
            }

            var coverFile = Path.ChangeExtension(_cbzFile, ".jpg");

            if (!string.IsNullOrEmpty(Settings.SaveCoverDir))
            {
                coverFile = Path.Combine(Settings.SaveCoverDir, Path.GetFileName(coverFile));
            }

            if (Settings.SaveCoverOnly)
            {
                ProgressReporter.Done(coverFile);
            }

            return coverFile;
        }

        public void WaitForPagesCompressed()
        {
            _jobWaiter.WaitForJobsToFinish();
            _compressor?.Dispose();
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
            var key = SharedSettings.GetPageString(_nextPageNumber);

            var firstPage = _nextPageNumber == 1;

            var imageList = new List<(string page, ArrayPoolBufferWriter<byte> imageData)>();

            while (_convertedPages.TryRemove(key, out var imageData))
            {
                imageList.Add((key, imageData));

                _pageNumbers.TryDequeue(out _nextPageNumber);

                key = SharedSettings.GetPageString(_nextPageNumber);
            }

            if (imageList.Count > 0)
            {
                var coverFile = firstPage ? _coverFile : null;

                var job = new ImageCompressorJob(_compressor, imageList, _progressReporter, coverFile);
                _compressorExecutor.AddJob(job);

                return true;
            }
            return false;
        }
    }
}
