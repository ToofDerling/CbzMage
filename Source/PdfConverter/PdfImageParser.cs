using CbzMage.Shared.Buffers;
using CbzMage.Shared.Extensions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfConverter.Exceptions;
using System.Buffers;

namespace PdfConverter
{
    public class PdfImageParser : IEventListener, IDisposable
    {
        // Largest image on a given page
        private readonly Dictionary<int, (int width, int height)> _imageMap;

        private readonly List<Exception> _imageParserErrors;

        private readonly Pdf _pdfComic;

        private readonly PdfReader _pdfReader;
        private readonly PdfDocument _pdfDoc;

        private int _pageNumber;
        private int _imageCount;

        private bool _pdfContainsRenderedText = false;

        private ICollection<EventType> _supportedEvents;

        private enum ImageMode
        {
            Parse, Save
        }

        private ImageMode _imageMode;

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

            _imageParserErrors = new List<Exception>();

            _imageMap = new Dictionary<int, (int width, int height)>();
        }

        public List<(int width, int height, int count)> ParseImages()
        {
            _supportedEvents = new[] { EventType.RENDER_IMAGE };

            _imageMode = ImageMode.Parse;

            if (_pdfComic.PageCount == 0)
            {
                throw new ApplicationException("Comic pageCount is 0");
            }

            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            for (_pageNumber = 1; _pageNumber <= _pdfComic.PageCount; _pageNumber++)
            {
                pdfDocParser.ProcessContent(_pageNumber, this);

                // Handle pages with no images
                if (!_imageMap.ContainsKey(_pageNumber))
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

        public List<Exception> GetImageParserErrors() => _imageParserErrors;

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
            switch (type)
            {
                case EventType.RENDER_IMAGE:
                    if (_imageMode == ImageMode.Save)
                    {
                        SaveImage((ImageRenderInfo)data);
                    }
                    else
                    {
                        ParseImage((ImageRenderInfo)data);
                    }
                    break;
                case EventType.RENDER_TEXT:
                    _pdfContainsRenderedText = true;
                    break;
            }
        }

        private void ParseImage(ImageRenderInfo renderInfo)
        {
            try
            {
                var imageObject = renderInfo.GetImage();

                var newWidth = Convert.ToInt32(imageObject.GetWidth());
                var newHeight = Convert.ToInt32(imageObject.GetHeight());

                // We want the largest image on any given page.
                if (!_imageMap.TryGetValue(_pageNumber, out var page) || (newWidth * newHeight > page.width * page.height))
                {
                    _imageMap[_pageNumber] = (newWidth, newHeight);
                }

                _imageCount++;
            }
            catch (Exception ex)
            {
                _imageParserErrors.Add(ex);
            }
        }

        private void SaveImage(ImageRenderInfo renderInfo)
        {
            try
            {
                var imageObject = renderInfo.GetImage();

                var imageBytes = imageObject.GetImageBytes(decoded: true);
                var imageExt = imageObject.IdentifyImageFileExtension();

                var bufferWriter = new ArrayPoolBufferWriter<byte>();
                bufferWriter.Write(imageBytes);

                _imageDataHandler.HandleSavedImageData(bufferWriter, imageExt);
            }
            catch (Exception ex)
            {
                _imageParserErrors.Add(ex);
            }
        }

        public ICollection<EventType> GetSupportedEvents() => _supportedEvents;

        public bool DetectRenderedText()
        {
            _supportedEvents = new[] { EventType.RENDER_TEXT };

            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            for (_pageNumber = 1; _pageNumber <= _pdfComic.PageCount; _pageNumber++)
            {
                pdfDocParser.ProcessContent(_pageNumber, this);

                if (_pdfContainsRenderedText)
                {
                    return true;
                }
            }

            return false;
        }

        private IImageDataHandler _imageDataHandler;

        public void SavePdfImages(List<int> pageList, IImageDataHandler imageDataHandler)
        {
            _imageDataHandler = imageDataHandler;

            _supportedEvents = new[] { EventType.RENDER_IMAGE };

            _imageMode = ImageMode.Save;

            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            for (int i = 0, sz = pageList.Count; i < sz; i++)
            {
                _pageNumber = pageList[i];

                pdfDocParser.ProcessContent(_pageNumber, this);
            }

            imageDataHandler.HandleSavedImageData(null!, "bye");
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
