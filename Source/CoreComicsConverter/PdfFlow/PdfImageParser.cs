using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;

namespace CoreComicsConverter.PdfFlow
{
    public class PdfImageParser : ComicPageParser, IEventListener, IDisposable
    {
        private Dictionary<int, ComicPage> _imageMap;

        private int _pageNumber;
        private int _imageCount;

        private readonly PdfComic _pdfComic;

        private readonly PdfReader _pdfReader;
        private readonly PdfDocument _pdfDoc;

        private readonly List<string> _parserWarnings;

        public PdfImageParser(PdfComic pdfComic)
        {
            _pdfComic = pdfComic;

            _pdfReader = new PdfReader(_pdfComic.Path);
            _pdfDoc = new PdfDocument(_pdfReader);

            if (_pdfReader.IsEncrypted())
            {
                throw new PdfEncryptedException($"{_pdfComic.Path} is encrypted.");
            }

            _pdfComic.PageCount = _pdfDoc.GetNumberOfPages();

            _parserWarnings = new List<string>();
        }

        public override event EventHandler<PageEventArgs> PageParsed;

        protected override List<ComicPage> Parse()
        {
            _imageMap = new Dictionary<int, ComicPage>();
            
            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            for (_pageNumber = 1; _pageNumber <= _pdfComic.PageCount; _pageNumber++)
            {
                pdfDocParser.ProcessContent(_pageNumber, this);

                // Handle pages with no images
                if (!_imageMap.TryGetValue(_pageNumber, out var _))
                {
                    _imageMap[_pageNumber] = new ComicPage { Number = _pageNumber };
                }

                PageParsed?.Invoke(this, new PageEventArgs(new ComicPage { Number = _pageNumber }));
            }

            _pdfComic.ImageCount = _imageCount;

            if (_imageMap.Count != _pdfComic.PageCount)
            {
                throw new ApplicationException($"{nameof(_imageMap)} is {_imageMap.Count} should be {_pdfComic.PageCount}");
            }

            return _imageMap.Values.AsList();
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            return new[] { EventType.RENDER_IMAGE };
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            try
            {
                var renderInfo = data as ImageRenderInfo;

                var imageObject = renderInfo.GetImage();

                var newWidth = Convert.ToInt32(imageObject.GetWidth());
                var newHeight = Convert.ToInt32(imageObject.GetHeight());

                if (_imageMap.TryGetValue(_pageNumber, out var page) && newWidth * newHeight > page.Width * page.Height)
                {
                    page.Width = newWidth;
                    page.Height = newHeight;
                }
                else
                {
                    _imageMap[_pageNumber] = new ComicPage { Number = _pageNumber, Width = newWidth, Height = newHeight };
                }

                _imageCount++;
            }
            catch (Exception ex)
            {
                _parserWarnings.Add(ex.Message);
            }
        }

        public List<string> GetParserWarnings()
        {
            return _parserWarnings;
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
