using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PdfConverter.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PdfConverter
{
    public class PdfParser : IRenderListener
    {
        private ConcurrentQueue<int> _pageQueue;

        private ConcurrentBag<(int width, int height)> _imageSizes;

        private List<Exception> _parserErrors;

        public void SetPageCount(Pdf pdf)
        {
            using (var pdfReader = new PdfReader(pdf.Path))
            {
                if (pdfReader.IsEncrypted())
                {
                    throw new ApplicationException(pdf.Path + " is encrypted.");
                }

                pdf.PageCount = pdfReader.NumberOfPages;
            }
        }

        public List<(int width, int height, int count)> ParseImages(Pdf pdf)
        {
            if (pdf.PageCount == 0)
            {
                SetPageCount(pdf);
            }

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

        #region empty IRenderListener methods

        public void BeginTextBlock()
        { }

        public void EndTextBlock()
        { }

        public void RenderText(TextRenderInfo renderInfo)
        { }

        #endregion
    }
}
