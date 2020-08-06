using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreComicsConverter.Images
{
    public class PdfImageParser : ImageParser, IEventListener
    {
        private Dictionary<int, (int pageNumber, int width, int height)> _imageMap;

        private int _pageNumber;

        private int _imageCount;

        private PdfReader _pdfReader;
        private PdfDocument _pdfDoc;

        public PdfImageParser(Comic pdfComic) : base(pdfComic)
        { 
        }

        public override event EventHandler<PageEventArgs> PageParsed;

        public override void OpenComicSetPageCount()
        {
            _pdfReader = new PdfReader(_comic.Path);
            _pdfDoc = new PdfDocument(_pdfReader);

            _comic.PageCount = _pdfDoc.GetNumberOfPages();
        }

        public override List<(int pageNumber, int width, int height)> ParsePagesSetImageCount()
        {
            _imageMap = new Dictionary<int, (int pageNumber, int width, int height)>();

            if (_pdfReader.IsEncrypted())
            {
                throw new PdfEncryptedException($"{_comic.Path} is encrypted.");
            }

            var pdfDocParser = new PdfDocumentContentParser(_pdfDoc);

            for (_pageNumber = 1; _pageNumber <= _comic.PageCount; _pageNumber++)
            {
                pdfDocParser.ProcessContent(_pageNumber, this);

                // Handle pages with no images
                if (!_imageMap.TryGetValue(_pageNumber, out var _))
                {
                    _imageMap[_pageNumber] = (_pageNumber, 0, 0);
                }

                PageParsed?.Invoke(this, new PageEventArgs( $"page {_pageNumber}"));
            }

            _comic.ImageCount = _imageCount;

            if (_imageMap.Count != _comic.PageCount)
            {
                throw new ApplicationException($"imageMap is {_imageMap.Count} should be {_comic.PageCount}");
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

                if (!_imageMap.TryGetValue(_pageNumber, out var old) || (newWidth * newHeight) > (old.width * old.height))
                {
                    _imageMap[_pageNumber] = (_pageNumber, newWidth, newHeight);
                }

                _imageCount++;
            }
            catch (Exception ex)
            {
                _parserWarnings.Add(ex.Message);
            }
        }

        public override void Dispose()
        {
            _pdfDoc?.Close();
            _pdfReader?.Close();
        }
    }
}
