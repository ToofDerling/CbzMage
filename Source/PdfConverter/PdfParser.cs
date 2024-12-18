using CbzMage.Shared.Extensions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfConverter.Exceptions;
using PdfConverter.PageInfo;
using System.Text;

namespace PdfConverter
{
    public class PdfParser : IEventListener, IDisposable
    {
        private readonly Dictionary<int, AbstractPdfPageInfo> _pageMap;
        private readonly List<(int pageNumber, Exception exception)> _imageParserErrors;

        // Does a page have rendered text
        private readonly HashSet<int> _pagesWithText;
        private readonly List<(int pageNumber, Exception exception)> _textParserErrors;

        private readonly Pdf _pdfComic;

        private readonly PdfReader _pdfReader;
        private readonly PdfDocument _pdfDoc;

        private int _pageNumber;
        private int _pdfImageCount;

        private EventType[] _supportedEvents;

        public PdfParser(Pdf pdfComic)
        {
            _pdfComic = pdfComic;

            _pdfReader = new PdfReader(_pdfComic.PdfPath);

            _pdfDoc = new PdfDocument(_pdfReader);

            if (_pdfReader.IsEncrypted())
            {
                throw new PdfEncryptedException();
            }

            _pdfComic.PageCount = _pdfDoc.GetNumberOfPages();

            _imageParserErrors = new List<(int pageNumber, Exception exception)>();
            _textParserErrors = new List<(int pageNumber, Exception exception)>();

            _pageMap = new Dictionary<int, AbstractPdfPageInfo>(_pdfComic.PageCount);
            _pagesWithText = new HashSet<int>();
        }

        public Dictionary<int, AbstractPdfPageInfo> GetPageMap() => _pageMap;

        public List<(int width, int height, int count)> AnalyzeImages()
        {
            _supportedEvents = new[] { EventType.RENDER_IMAGE };

            if (_pdfComic.PageCount == 0)
            {
                throw new ApplicationException("Comic pageCount is 0");
            }

            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            for (_pageNumber = 1; _pageNumber <= _pdfComic.PageCount; _pageNumber++)
            {
                try
                {
                    pdfDocParser.ProcessContent(_pageNumber, this);
                }
                catch (Exception ex)
                {
                    _imageParserErrors.Add((_pageNumber, ex));
                }

                // Handle pages with no images
                if (!_pageMap.ContainsKey(_pageNumber))
                {
                    _pageMap[_pageNumber] = new PdfPageInfoRenderImage(_pageNumber);
                }

                PageParsed?.Invoke(this, new PageParsedEventArgs(_pageNumber, ParserMode.Images));
            }

            _pdfComic.ImageCount = _pdfImageCount;

            if (_pageMap.Count != _pdfComic.PageCount)
            {
                throw new ApplicationException($"{nameof(_pageMap)} is {_pageMap.Count} should be {_pdfComic.PageCount}");
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

        public List<(int pageNumber, Exception exception)> GetImageParserErrors() => _imageParserErrors;

        public List<(int pageNumber, Exception exception)> GetTextParserErrors() => _textParserErrors;

        private Dictionary<string, (int width, int height, int count)> BuildImageSizesMap()
        {
            var imageSizesMap = new Dictionary<string, (int, int, int count)>(_pdfComic.PageCount);

            foreach (var page in _pageMap.Values)
            {
                var key = $"{page.LargestImage.width} x {page.LargestImage.height}";

                var count = imageSizesMap.TryGetValue(key, out var existingImageSize)
                    ? existingImageSize.count + 1
                    : 1;

                imageSizesMap[key] = (page.LargestImage.width, page.LargestImage.height, count);
            }

            return imageSizesMap;
        }

        public event EventHandler<PageParsedEventArgs> PageParsed;

        public void EventOccurred(IEventData data, EventType type)
        {
            switch (type)
            {
                case EventType.RENDER_IMAGE:
                    ParseImage((ImageRenderInfo)data);
                    break;
                case EventType.RENDER_TEXT:
                    _pagesWithText.Add(_pageNumber);
                    break;
            }
        }

        private void ParseImage(ImageRenderInfo renderInfo)
        {
            var imageObject = renderInfo.GetImage();

            var newWidth = imageObject.GetWidth().ToInt();
            var newHeight = imageObject.GetHeight().ToInt();

            if (!_pageMap.TryGetValue(_pageNumber, out var page))
            {
                page = new PdfPageInfoRenderImage(_pageNumber);
                _pageMap[page.PageNumber] = page;
            }

            page.ImageCount++;

            // We want the largest image on any given page.
            if (newWidth * newHeight > page.LargestImage.width * page.LargestImage.height)
            {
                page.LargestImage = (newWidth, newHeight);
                page.LargestImageExt = imageObject.IdentifyImageFileExtension();
            }

            if (page.PageSize.width == 0)
            {
                var pdfPage = _pdfDoc.GetPage(_pageNumber);
                var pageSize = pdfPage.GetPageSize();

                page.PageSize = (pageSize.GetWidth().ToInt(), pageSize.GetHeight().ToInt());
            }

            _pdfImageCount++;
        }

        public ICollection<EventType> GetSupportedEvents() => _supportedEvents;

        public List<AbstractPdfPageInfo> FilterPagesWithText(List<AbstractPdfPageInfo> pageInfos)
        {
            _supportedEvents = new[] { EventType.BEGIN_TEXT, EventType.END_TEXT, EventType.RENDER_TEXT };

            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            foreach (var pageInfo in pageInfos)
            {
                _pageNumber = pageInfo.PageNumber;

                try
                {
                    pdfDocParser.ProcessContent(_pageNumber, this);
                }
                catch (Exception ex)
                {
                    // Text rendering can fail because of a missing font. Collect the error and mark the page as having rendered text
                    _pagesWithText.Add(_pageNumber);

                    _imageParserErrors.Add((_pageNumber, ex));
                }

                // Try harder
                if (!_pagesWithText.Contains(_pageNumber))
                {
                    var page = _pdfDoc.GetPage(_pageNumber);
                    var contentStream = page.GetFirstContentStream();

                    var bytes = contentStream.GetBytes();
                    var text = Encoding.UTF8.GetString(bytes);

                    if (text.ContainsIgnoreCase("/PlacedGraphic"))
                    {
                        Console.WriteLine($"{_pageNumber} -> /PlacedGraphic");
                        _pagesWithText.Add(_pageNumber);
                    }
                    else if (text.ContainsIgnoreCase("/PlacedPDF"))
                    {
                        Console.WriteLine($"{_pageNumber} -> /PlacedPDF");
                        _pagesWithText.Add(_pageNumber);
                    }
                }

                //PageParsed?.Invoke(this, new PageParsedEventArgs(_pageNumber, ParserMode.Text));
            }

            var pagesWithoutText = new List<AbstractPdfPageInfo>();

            foreach (var pageInfo in pageInfos)
            {
                if (!_pagesWithText.Contains(pageInfo.PageNumber))
                {
                    pagesWithoutText.Add(pageInfo);
                }
            }

            return pagesWithoutText;
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
