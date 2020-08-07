using CoreComicsConverter.Model;
using ImageMagick;
using System;
using System.IO;

namespace CoreComicsConverter
{
    public class DpiCalculator
    {
        private readonly int _wantedImageWidth;

        private readonly PdfComic _pdfComic;

        private readonly (int number, int width, int height) _page;

        private string _currentPage;

        private (int width, int height) _currentImageSize;

        public DpiCalculator(PdfComic pdfComic, (int number, int width, int height) page)
        {
            _page = page;

            _wantedImageWidth = _page.width;

            _pdfComic = pdfComic;
        }

        public int CalculateDpi()
        {
            var dpi = Settings.MinimumDpi;

            var width = GetImageWidth(dpi);

            if (width < _wantedImageWidth)
            {
                dpi = GoingUp(dpi, width, _wantedImageWidth);
            }

            return dpi;
        }

        public string GetCurrentPage()
        {
            return _currentPage;
        }

        public (int width, int height) GetCurrentImageSize()
        {
            return _currentImageSize;
        }

        private int GoingUp(int dpi, int width, int wantedWidth)
        {
            var diff = wantedWidth - width;

            // Skip big step calculation if difference is small
            if (diff <= 15)
            {
                var nextWidth = width;
                while (nextWidth < wantedWidth)
                {
                    dpi++;
                    nextWidth = GetImageWidth(dpi);
                }
            }
            else
            {
                var step = 5;
                dpi += step;
                var nextWidth = GetImageWidth(dpi);

                // Calculate big step if next width produced a large enough difference 
                if (nextWidth < wantedWidth && wantedWidth - nextWidth > 15)
                {
                    var nextDiff = wantedWidth - nextWidth;
                    var bigStep = CalculateBigStep(diff, nextDiff, step);

                    dpi = Settings.MinimumDpi + bigStep;
                    nextWidth = GetImageWidth(dpi);
                }
                // Go down if big step put us above wanted width
                // Go up if after big step we're still below wanted width, or the calculation was skipped
                dpi = GoDownAndUp(dpi, nextWidth, wantedWidth);
            }

            return dpi;
        }

        private int GoDownAndUp(int dpi, int nextWidth, int wantedWidth)
        {
            while (nextWidth > wantedWidth && nextWidth - wantedWidth > 5)
            {
                dpi--;
                nextWidth = GetImageWidth(dpi);
            }

            while (nextWidth < wantedWidth)
            {
                dpi++;
                nextWidth = GetImageWidth(dpi);
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

        private int GetImageWidth(int dpi)
        {
            var pageMachine = new GhostscriptPageMachine();

            pageMachine.ReadPage(_pdfComic, _page.number, dpi);

            _currentPage = $"{_page.number}-1.png";
            var pagePath = Path.Combine(_pdfComic.OutputDirectory, _currentPage);

            using var image = new MagickImage();

            image.Ping(pagePath);
            _currentImageSize = (image.Width, image.Height);

            return _currentImageSize.width;
        }
    }
}
