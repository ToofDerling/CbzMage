using CbzMage.Shared.Helpers;
using PdfConverter.ImageData;
using PdfConverter.PageMachines;

namespace PdfConverter.ImageProducer
{
    public class PopplerImageProducer : AbstractImageProducer
    {
        private readonly int _dpi;

        public PopplerImageProducer(Pdf pdf, List<int> pageList, int dpi) : base(pdf, pageList)
        {
            _dpi = dpi;
        }

        private ProcessRunner _gsRunner;

        public override void Start(IImageDataHandler imageDataHandler)
        {
            var pageMachine = new PopplerPageMachine();
            _gsRunner = pageMachine.StartReadingPages(Pdf, PageList, _dpi, imageDataHandler);

            IsStarted = true;
        }

        public override ICollection<string> GetErrors()
        {
            EnsureStarted();
            return _gsRunner.GetStandardErrorLines();
        }

        public override int WaitForExit()
        {
            EnsureStarted();
            return _gsRunner.WaitForExitCode();
        }

        public override void Dispose()
        {
            EnsureStarted();
            _gsRunner.Dispose();
        }
    }
}
