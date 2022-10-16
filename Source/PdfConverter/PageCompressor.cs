﻿using ImageMagick;
using PdfConverter.Ghostscript;
using PdfConverter.Helpers;
using PdfConverter.Jobs;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;

namespace PdfConverter
{
    public class PageCompressor
    {
        private readonly Pdf _pdf;

        private readonly ConcurrentQueue<int> _pageNumbers;

        private readonly ConcurrentDictionary<string, MagickImage> _convertedPages;

        private readonly JobExecutor<IEnumerable<string>> _compressorExecutor;

        private readonly JobWaiter _jobWaiter;

        private readonly ZipArchive _compressor;

        private bool _addedJob = false;

        private int _nextPageNumber;

        private readonly string _cbzFile;

        public PageCompressor(Pdf pdf, ConcurrentDictionary<string, MagickImage> convertedPages)
        {
            _pdf = pdf;
            _convertedPages = convertedPages;

            _pageNumbers = new ConcurrentQueue<int>(Enumerable.Range(1, pdf.PageCount));
            _pageNumbers.TryDequeue(out _nextPageNumber);

            _compressorExecutor = new JobExecutor<IEnumerable<string>>(ThreadPriority.AboveNormal);
            _compressorExecutor.JobExecuted += (s, e) => OnImagesCompressed(e);

            _jobWaiter = _compressorExecutor.Start(withWaiter: true);

            _cbzFile = Path.ChangeExtension(pdf.Path, ".cbz");
            Console.WriteLine(Path.GetFileName(_cbzFile));

            File.Delete(_cbzFile);
            _compressor = ZipFile.Open(_cbzFile, ZipArchiveMode.Create);
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

            var inputMap = new SortedDictionary<string, MagickImage>();

            while (_convertedPages.TryRemove(key, out var image))
            {
                inputMap.Add(key, image);

                _pageNumbers.TryDequeue(out _nextPageNumber);

                key = _pdf.GetPageString(_nextPageNumber);
            }

            if (inputMap.Count > 0)
            {
                var job = new ImagesCompressorJob(_compressor, inputMap);
                _compressorExecutor.AddJob(job);

                return true;
            }
            return false;
        }
    }
}