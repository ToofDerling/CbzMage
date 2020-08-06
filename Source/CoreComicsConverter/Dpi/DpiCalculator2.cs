using CoreComicsConverter.Model;
using ImageMagick;
using System;
using System.IO;

namespace CoreComicsConverter.Dpi
{

    public class DpiCalculator2
    {
        private readonly (int width, int height) _wantedImageSize;

        private readonly PdfComic _pdfComic;

        private readonly int _pageNumber;

        private const int DpiStep = 5;
        private const int SizeThreshold = 30; 
        private const int SmallSizeThreshold = 5;

        public DpiCalculator2(PdfComic pdfComic, (int pageNumber, int width, int height) page)
        {
            _wantedImageSize = (page.width, page.height);

            _pdfComic = pdfComic;

            _pageNumber = page.pageNumber;
        }

        public int CalculateDpi()
        {
            var dpi = Settings.MinimumDpi;

            var size = GetImageWidth(dpi);

            if (size.IsSmallerThan(_wantedImageSize))
            {
                dpi = GoingUp(dpi, size, _wantedImageSize);
            }

            return dpi;
        }

        private int GoingUp(int dpi, (int width, int height) imageSize, (int width, int height) wantedImageSize)
        {
            var diff = wantedImageSize.GetDifference(imageSize);

            // Skip big step calculation if difference is small
            if (diff <= SizeThreshold)
            {
                var nextImageSize = imageSize;
                while (nextImageSize.IsSmallerThan(wantedImageSize))
                {
                    dpi++;
                    nextImageSize = GetImageWidth(dpi);
                }
            }
            else
            {
                dpi += DpiStep;
                var nextImageSize = GetImageWidth(dpi);

                // Calculate big step if next width produced a large enough difference 
                var nextDiff = wantedImageSize.GetDifference(nextImageSize);
                if (nextDiff > SizeThreshold)
                {
                    var bigStep = CalculateBigStep(diff, nextDiff, DpiStep);

                    dpi = Settings.MinimumDpi + bigStep;
                    nextImageSize = GetImageWidth(dpi);
                }
                // Go down if big step put us above wanted width
                // Go up if after big step we're still below wanted width, or the calculation was skipped
                dpi = GoDownAndUp(dpi, nextImageSize, wantedImageSize);
            }

            return dpi;
        }

        private int GoDownAndUp(int dpi, (int width, int height) nextImageSize, (int width, int height) wantedImageSize)
        {
            while (nextImageSize.GetDifference(wantedImageSize) > SmallSizeThreshold)
            {
                dpi--;
                nextImageSize = GetImageWidth(dpi);
            }

            while (nextImageSize.IsSmallerThan(wantedImageSize))
            {
                dpi++;
                nextImageSize = GetImageWidth(dpi);
            }

            return dpi;
        }

        private static int CalculateBigStep(int firstDiff, int nextDiff, int usedStep)
        {
            var interval = (double)firstDiff - nextDiff;
            var factor = firstDiff / interval;

            var bigStep = usedStep * factor;
            return Convert.ToInt32(bigStep);
        }

        private (int width, int height) GetImageWidth(int dpi)
        {
            var pageMachine = new GhostscriptPageMachine();

            pageMachine.ReadPage(_pdfComic, _pageNumber, dpi);

            var page = _pdfComic.GetPngPageString(1);
            var pagePath = Path.Combine(_pdfComic.OutputDirectory, page);

            (int width, int height) imageSize;
            using (var image = new MagickImage())
            {
                image.Ping(pagePath);
                imageSize = (image.Width, image.Height);
            };

            DpiCalculated?.Invoke(this, new DpiCalculatedEventArgs2(dpi, Settings.MinimumDpi, imageSize));

            return imageSize;
        }

        public event EventHandler<DpiCalculatedEventArgs2> DpiCalculated;
    }
}
