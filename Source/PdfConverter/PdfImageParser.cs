using CbzMage.Shared.Extensions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfConverter.Exceptions;
using PdfConverter.Helpers;

namespace PdfConverter
{
    public class PdfImageParser : IEventListener, IDisposable
    {
        // Largest image on a given page
        private readonly Dictionary<int, (int width, int height)> _imageMap;

        private readonly List<Exception> _parserErrors;

        private readonly Pdf _pdfComic;

        private readonly PdfReader _pdfReader;
        private readonly PdfDocument _pdfDoc;

        private int _pageNumber;
        private int _imageCount;

        public PdfImageParser(Pdf pdfComic)
        {
            _pdfComic = pdfComic;

            _pdfReader = new PdfReader(_pdfComic.Path);
            _pdfDoc = new PdfDocument(_pdfReader);

            if (_pdfReader.IsEncrypted())
            {
                throw new PdfEncryptedException();
            }

            _pdfComic.PageCount = _pdfDoc.GetNumberOfPages();

            _parserErrors = new List<Exception>();
        
            _imageMap = new Dictionary<int, (int width, int height)>();
        }

        public List<(int width, int height, int count)> ParseImages()
        {
            if (_pdfComic.PageCount == 0)
            {
                throw new ApplicationException("Comic pageCount is 0");
            }

            var progressReporter = new ProgressReporter(_pdfComic.PageCount);
            PageParsed += (s, e) => progressReporter.ShowProgress($"Parsing page {e.CurrentPage}");

            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            for (_pageNumber = 1; _pageNumber <= _pdfComic.PageCount; _pageNumber++)
            {
                pdfDocParser.ProcessContent(_pageNumber, this);

                // Handle pages with no images
                if (!_imageMap.TryGetValue(_pageNumber, out var _))
                {
                    _imageMap[_pageNumber] = (0, 0);
                }

                PageParsed?.Invoke(this, new PageParsedEventArgs(_pageNumber));
            }
            _pdfComic.ImageCount = _imageCount;

            if (_imageMap.Count != _pdfComic.PageCount)
            {
                throw new ApplicationException($"{nameof(_imageMap)} is {_imageMap.Count} should be {_pdfComic.PageCount}");
            }

            var imageSizesMap = BuildImageSizesMap();
            var sortedImagesList = imageSizesMap.Values.OrderByDescending(x => x.count).AsList();

            var pageSum = sortedImagesList.Sum(i => i.count);
            if (pageSum != _pdfComic.PageCount)
            {
                throw new ApplicationException($"{nameof(sortedImagesList)} pageSum {pageSum} should be {_pdfComic.PageCount}");
            }

            return sortedImagesList;
        }

        public List<Exception> GetImageParserErrors()
        {
            return _parserErrors ?? new List<Exception>();
        }

        private Dictionary<string, (int width, int height, int count)> BuildImageSizesMap()
        {
            var imageSizesMap = new Dictionary<string, (int, int, int count)>();

            foreach (var (width, height) in _imageMap.Values)
            {
                var key = $"{width} x {height}";

                var count = imageSizesMap.TryGetValue(key, out var existingImageSize)
                    ? existingImageSize.count + 1
                    : 1;

                imageSizesMap[key] = (width, height, count);
            }

            return imageSizesMap;
        }

        public event EventHandler<PageParsedEventArgs> PageParsed;

        public void EventOccurred(IEventData data, EventType type)
        {
            try
            {
                var renderInfo = data as ImageRenderInfo;
                var imageObject = renderInfo.GetImage();

                var newWidth = Convert.ToInt32(imageObject.GetWidth());
                var newHeight = Convert.ToInt32(imageObject.GetHeight());

                // We want the largest image on any given page.
                if (!_imageMap.TryGetValue(_pageNumber, out var page)
                    || (newWidth * newHeight > page.width * page.height))
                {
                    _imageMap[_pageNumber] = (newWidth, newHeight);
                }

                _imageCount++;
            }
            catch (Exception ex)
            {
                _parserErrors.Add(ex);
            }
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            return new[] { EventType.RENDER_IMAGE };
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
