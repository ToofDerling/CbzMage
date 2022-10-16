using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfConverter.Exceptions;
using PdfConverter.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PdfConverter
{
    public class PdfParser : IEventListener, IDisposable
    {
        private ConcurrentQueue<int> _pageQueue;

        private ConcurrentBag<(int width, int height)> _imageSizes;

        private List<Exception> _parserErrors;

        private readonly Pdf _pdfComic;

        private readonly PdfReader _pdfReader;
        private readonly PdfDocument _pdfDoc;

        private readonly List<string> _parserWarnings;

        public PdfParser(Pdf pdfComic)
        {
            _pdfComic = pdfComic;

            _pdfReader = new PdfReader(_pdfComic.Path);
            _pdfDoc = new PdfDocument(_pdfReader);

            if (_pdfReader.IsEncrypted())
            {
                throw new PdfEncryptedException();
            }

            _pdfComic.PageCount = _pdfDoc.GetNumberOfPages();

            _parserWarnings = new List<string>();
        }

        public List<(int width, int height, int count)> ParseImages(Pdf pdf)
        {
            var pages = Enumerable.Range(1, pdf.PageCount);
            _pageQueue = new ConcurrentQueue<int>(pages);

            _imageSizes = new ConcurrentBag<(int, int)>();
            _parserErrors = new List<Exception>();

            Parallel.For(0, Environment.ProcessorCount, (index, state) => ProcessPages(pdf, state));

            pdf.ImageCount = _imageSizes.Count;

            var imageSizesMap = BuildImageSizesMap();

            var list = imageSizesMap.Values.OrderByDescending(x => x.count).AsList();
            return list;
        }

        public List<Exception> GetImageParserErrors()
        {
            return _parserErrors ?? new List<Exception>();
        }

        private Dictionary<string, (int width, int height, int count)> BuildImageSizesMap()
        {
            var imageSizesMap = new Dictionary<string, (int, int, int count)>();

            foreach (var (width, height) in _imageSizes)
            {
                var key = $"{width} x {height}";

                var count = imageSizesMap.TryGetValue(key, out var existingImageSize) ? existingImageSize.count + 1 : 1;

                imageSizesMap[key] = (width, height, count);
            }

            return imageSizesMap;
        }

        private void ProcessPages(Pdf pdf, ParallelLoopState loopState)
        {
            if (_pageQueue.IsEmpty)
            {
                loopState.Stop();
                return;
            }

            using (var pdfReader = new PdfReader(pdf.Path))
            {
                var pdfParser = new PdfReaderContentParser(pdfReader);

                while (!_pageQueue.IsEmpty)
                {
                    if (_pageQueue.TryDequeue(out var currentPage))
                    {
                        pdfParser.ProcessContent(currentPage, this);

                        PageParsed?.Invoke(this, new PageParsedEventArgs(currentPage));
                    }
                }
            }
        }

        public event EventHandler<PageParsedEventArgs> PageParsed;

        public void RenderImage(ImageRenderInfo renderInfo)
        {
            try
            {
                var imageObject = renderInfo.GetImage();

                var width = imageObject.Get(PdfName.WIDTH);
                var height = imageObject.Get(PdfName.HEIGHT);

                _imageSizes.Add((width.ToInt(), height.ToInt()));
            }
            catch (Exception ex)
            {
                _parserErrors.Add(ex);
            }
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            throw new NotImplementedException();
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            throw new NotImplementedException();
        }

        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _pdfReader?.Close();
                    _pdfDoc?.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PdfImageParser()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        
        #endregion
    }
}
