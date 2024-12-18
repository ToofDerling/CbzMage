using CbzMage.Shared.Buffers;
using CbzMage.Shared.IO;
using ImageMagick;
using PdfConverter.Helpers;
using PdfConverter.PageInfo;
using PdfConverter.PageMachines;

namespace PdfConverter.ImageConversion
{
    public class PopplerSaveImageConverter : AbstractImageConverter
    {
        private readonly PdfPageInfoSaveImage _pdfPageInfoSaveImage;

        private long _imageSize;

        public PopplerSaveImageConverter(Pdf pdf, PdfPageInfoSaveImage pdfPageInfo) : base(pdf, pdfPageInfo)
        {
            _pdfPageInfoSaveImage = pdfPageInfo;
        }

        public override void OpenImage()
        {
            var pageMachine = new PopplerSavePageMachine();

            ProcessRunner = pageMachine.SavePage(Pdf, _pdfPageInfoSaveImage, Pdf.SaveDirectory);
            _pdfPageInfoSaveImage.SavedImagePath = pageMachine.GetSavedPagePath();

            IsStarted = true;
        }

        public async override Task ConvertImageAsync()
        {
            WaitForExit();

            // Don't touch saved jpgs, unless they're marked for resizing
            if (_pdfPageInfoSaveImage.IsJpg() && !AbstractPdfPageInfo.UseResizeHeight(_pdfPageInfoSaveImage.LargestImage.height, GetResizeHeight()))
            {
                return;
            }

            ConvertedImageData = new ArrayPoolBufferWriter<byte>(Settings.ImageBufferSize);

            var imageWasResized = false;

            using (var image = new MagickImage())
            {
                using (var stream = GetImageStream())
                {
                    await image.ReadAsync(stream);
                }

                if (_pdfPageInfoSaveImage.IsJpg())
                {
                    image.Format = MagickFormat.Jpg;
                    image.Quality = Settings.JpgQuality;
                }
                else
                {
                    image.Format = MagickFormat.Png;
                    image.Quality = 100;

                    SetImageExt(PdfImageExt.Png); // in case it's .jp2, .tiff etc
                }

                imageWasResized = ResizeImage(image);

                image.Write(ConvertedImageData);
            }

            File.Delete(GetSavedImagePath());

            DebugStatsCount.AddResize(imageWasResized);
        }

        public override Stream GetImageStream()
        {
            EnsureStarted();

            var fileStream = AsyncStreams.AsyncFileReadStream(GetSavedImagePath());
            _imageSize = fileStream.Length;

            return fileStream;
        }

        public long GetImageSize() => _imageSize;

        public string GetSaveDirectory() => Pdf.SaveDirectory;

        public string GetSavedImagePath() => _pdfPageInfoSaveImage.SavedImagePath;

        public override void CloseImage()
        {
            if (ConvertedImageData != null)
            {
                ConvertedImageData.Close();
            }
            else
            {
                File.Delete(GetSavedImagePath());
            }
        }
    }
}
