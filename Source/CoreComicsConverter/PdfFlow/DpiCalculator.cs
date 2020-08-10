using CoreComicsConverter.Model;
using ImageMagick;
using System;
using System.IO;

namespace CoreComicsConverter.PdfFlow
{
    public class DpiCalculator
    {
        private readonly int _wantedImageWidth;

        private readonly PdfComic _pdfComic;

        private readonly Page _page;

        public DpiCalculator(PdfComic pdfComic, PageBatch batch, Page page)
        {
            _page = page;

            _wantedImageWidth = batch.Width;

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

            pageMachine.ReadPage(_pdfComic, _page.Number, dpi);

            if (string.IsNullOrEmpty(_page.Path))
            {
                _page.Name = _pdfComic.GetSinglePagePngString(_page.Number);
                _page.Path = Path.Combine(_pdfComic.OutputDirectory, _page.Name);
            }

            using var image = new MagickImage();
            image.Ping(_page.Path);

            _page.Width = image.Width;
            _page.Height = image.Height;

            return _page.Width;
        }
    }
}
