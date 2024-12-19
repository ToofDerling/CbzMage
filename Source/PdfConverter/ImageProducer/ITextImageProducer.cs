using CbzMage.Shared.Extensions;

namespace PdfConverter.ImageProducer
{
    public class ITextImageProducer : AbstractImageProducer
    {
        public ITextImageProducer(Pdf pdf, List<int> pageNumbers) : base(pdf, pageNumbers)
        {
        }

        private PdfImageParser _pdfImageParser;

        public override void Start(IImageDataHandler imageDataHandler)
        {
            _pdfImageParser = new PdfImageParser(Pdf);
            _pdfImageParser.SavePdfImages(PageList, imageDataHandler);

            IsStarted = true;
        }

        public override ICollection<string> GetErrors()
        {
            EnsureStarted();
            return _pdfImageParser.GetImageParserErrors().Select(e => e.TypeAndMessage()).ToList();
        }

        public override int WaitForExit()
        {
            EnsureStarted();
            return _pdfImageParser.GetImageParserErrors().Count;
        }

        public override void Dispose()
        {
            EnsureStarted();
            _pdfImageParser.Dispose();
        }
    }
}
