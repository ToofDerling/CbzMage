using CbzMage.Shared.Buffers;
using ImageMagick;
using PdfConverter.Helpers;
using PdfConverter.PageInfo;
using PdfConverter.PageMachines;
using System.Diagnostics;

namespace PdfConverter.ImageConversion
{
    public class PopplerRenderImageConverter : AbstractImageConverter
    {
        private readonly PdfPageInfoRenderImage _pageInfoRenderImage;

        public PopplerRenderImageConverter(Pdf pdf, PdfPageInfoRenderImage pdfPageInfoRender) : base(pdf, pdfPageInfoRender)
        {
            _pageInfoRenderImage = pdfPageInfoRender;
        }

        public override void OpenImage()
        {
            var pageMachine = new PopplerRenderPageMachine();
            ProcessRunner = pageMachine.RenderPage(Pdf, _pageInfoRenderImage);

            IsStarted = true;
        }

        public override async Task ConvertImageAsync()
        {
            EnsureStarted();

#if DEBUG 
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var imageWasResized = false;
            var writtenCount = 0;

            ConvertedImageData = new ArrayPoolBufferWriter<byte>(Settings.ImageBufferSize);

            using var stream = GetImageStream();

            int readCount = 0;

            while (true)
            {
                var memory = ConvertedImageData.GetMemory(Settings.ReadRequestSize);

                readCount = await stream.ReadAsync(memory);
                if (readCount == 0)
                {
                    break;
                }

                DebugStatsCount.AddReadCount(readCount);
      
                ConvertedImageData.Advance(readCount);
            }

            WaitForExit();

            writtenCount = ConvertedImageData.WrittenCount;

            using var image = new MagickImage(ConvertedImageData.WrittenSpan);

            // Produce baseline jpgs with no subsampling.
            image.Format = MagickFormat.Jpg;
            image.Quality = Settings.JpgQuality;

            imageWasResized = ResizeImage(image);

            // Reuse the png buffer for the jpg
            ConvertedImageData.Reset();

            image.Write(ConvertedImageData);

            SetImageExt(PdfImageExt.Jpg);

#if DEBUG
            stopwatch.Stop();
            DebugStatsCount.AddWrittenCount((int)stopwatch.ElapsedMilliseconds, writtenCount, imageWasResized);
#endif

        }

        public override void CloseImage()
        {
            ConvertedImageData?.Close();
        }
    }
}
