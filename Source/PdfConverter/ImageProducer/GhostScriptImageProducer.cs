using CbzMage.Shared.Helpers;
using PdfConverter.Ghostscript;

namespace PdfConverter.ImageProducer
{
    public class GhostScriptImageProducer : AbstractImageProducer
    {
        private readonly int _dpi;

        public GhostScriptImageProducer(Pdf pdf, List<int> pageList, int dpi) : base(pdf, pageList)
        {
            _dpi = dpi;
        }

        private ProcessRunner _gsRunner;

        public override void Start(IImageDataHandler imageDataHandler)
        {
            var pageMachine = new GhostscriptPageMachine();
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
