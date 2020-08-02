using ImageMagick;
using Rotvel.PdfConverterCore;
using System;
using System.IO;

namespace Rotvel.PdfConverter
{
    public class DpiCalculator
    {
        private readonly int _wantedImageWidth;

        private readonly Pdf _pdf;

        public DpiCalculator(Pdf pdf, int wantedImageWidth)
        {
            _wantedImageWidth = wantedImageWidth;

            _pdf = pdf;
        }

        public int CalculateDpi()
        {
            var dpi = Program.QualityConstants.MinimumDpi;

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

                    dpi = Program.QualityConstants.MinimumDpi + bigStep;
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

        private int CalculateBigStep(int firstDiff, int nextDiff, int usedStep)
        {
            var interval = (double)firstDiff - nextDiff;
            var factor = firstDiff / interval;

            var bigStep = usedStep * factor;
            return Convert.ToInt32(bigStep);
        }

        private int GetImageWidth(int dpi)
        {
            var pageMachine = new GhostscriptPageMachine();

            pageMachine.ReadFirstPage(_pdf, dpi);

            var page = _pdf.GetPngPageString(1);
            var pagePath = Path.Combine(_pdf.OutputDirectory, page);

            int width;
            using (var image = new MagickImage())
            {
                image.Ping(pagePath);
                width = image.Width;
            };

            DpiCalculated?.Invoke(this, new DpiCalculatedEventArgs(dpi, Program.QualityConstants.MinimumDpi, width));
            return width;
        }

        public event EventHandler<DpiCalculatedEventArgs> DpiCalculated;
    }
}
