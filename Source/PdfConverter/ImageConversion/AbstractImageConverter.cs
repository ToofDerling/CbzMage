using CbzMage.Shared.Buffers;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using ImageMagick;
using PdfConverter.PageInfo;

namespace PdfConverter.ImageConversion
{
    public abstract class AbstractImageConverter
    {
        public Pdf Pdf { get; private set; }

        public AbstractPdfPageInfo PdfPageInfo { get; private set; }

        protected bool IsStarted { get; set; }

        protected ProcessRunner ProcessRunner { get; set; }

        private readonly List<string> _errorLines;


        public AbstractImageConverter(Pdf pdf, AbstractPdfPageInfo pdfPageInfo)
        {
            Pdf = pdf;
            PdfPageInfo = pdfPageInfo;

            _errorLines = new List<string>();
        }

        protected void EnsureStarted()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("Start() not called!");
            }
        }

        public int? GetResizeHeight() => PdfPageInfo.ResizeHeight;

        protected bool ResizeImage(MagickImage image)
        {
            var imageWasResized = false;

            var resizeHeight = GetResizeHeight();

            if (AbstractPdfPageInfo.UseResizeHeight(image.Height, resizeHeight))
            {
                imageWasResized = true;

                image.Resize(new MagickGeometry
                {
                    Greater = true,
                    Less = false,
                    Height = resizeHeight!.Value
                });
            }

            return imageWasResized;
        }

        public string GetPageString() => PdfPageInfo.PageNumber.ToPageString(PdfPageInfo.LargestImageExt);

        public int GetPageNumber() => PdfPageInfo.PageNumber;

        public string GetImageExt() => PdfPageInfo.LargestImageExt;

        public void SetImageExt(string imageExt) => PdfPageInfo.LargestImageExt = imageExt;

        public ArrayPoolBufferWriter<byte>? ConvertedImageData { get; set; }

        public abstract void OpenImage();

        public abstract Task ConvertImageAsync();

        public List<string> GetErrorLines() => _errorLines;

        public int WaitForExit()
        {
            EnsureStarted();

            var exitCode = ProcessRunner.WaitForExitCode();
            if (exitCode != 0)
            {
                _errorLines.AddRange(ProcessRunner.GetStandardErrorLines());
            }
            ProcessRunner.DisposeDontCare();

            return exitCode;
        }

        public virtual Stream GetImageStream()
        {
            EnsureStarted();
            return ProcessRunner.GetOutputStream();
        }

        public abstract void CloseImage();
    }
}
