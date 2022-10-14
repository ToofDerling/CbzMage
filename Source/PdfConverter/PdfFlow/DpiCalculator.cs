using PdfConverter.Model;
using System;
using System.IO;

namespace PdfConverter.PdfFlow
{
    public class DpiCalculator
    {
        private readonly int _wantedImageHeight;

        private readonly PdfComic _pdfComic;

        private readonly ComicPage _page;

        public DpiCalculator(PdfComic pdfComic, int wantedHeight, ComicPage page)
        {
            _page = page;

            _wantedImageHeight = wantedHeight;

            _pdfComic = pdfComic;
        }

        public int CalculateDpi()
        {
            var dpi = Settings.MinimumDpi;

            var height = GetImageHeight(dpi);

            if (height < _wantedImageHeight)
            {
                dpi = GoingUp(dpi, height, _wantedImageHeight);
            }

            return dpi;
        }

        private int GoingUp(int dpi, int height, int wantedHeight)
        {
            var diff = wantedHeight - height;

            // Skip big step calculation if difference is small
            if (diff <= 15)
            {
                var nextHeight = height;
                while (nextHeight < wantedHeight)
                {
                    dpi++;
                    nextHeight = GetImageHeight(dpi);
                }
            }
            else
            {
                var step = 5;
                dpi += step;
                var nextHeight = GetImageHeight(dpi);

                // Calculate big step if next height produced a large enough difference 
                if (nextHeight < wantedHeight && wantedHeight - nextHeight > 15)
                {
                    var nextDiff = wantedHeight - nextHeight;
                    var bigStep = CalculateBigStep(diff, nextDiff, step);

                    dpi = Settings.MinimumDpi + bigStep;
                    nextHeight = GetImageHeight(dpi);
                }
                // Go down if big step put us above wanted height
                // Go up if after big step we're still below wanted height, or the calculation was skipped
                dpi = GoDownAndUp(dpi, nextHeight, wantedHeight);
            }

            return dpi;
        }

        private int GoDownAndUp(int dpi, int nextHeight, int wantedHeight)
        {
            while (nextHeight > wantedHeight && nextHeight - wantedHeight > 5)
            {
                dpi--;
                nextHeight = GetImageHeight(dpi);
            }

            while (nextHeight < wantedHeight)
            {
                dpi++;
                nextHeight = GetImageHeight(dpi);
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

        private int GetImageHeight(int dpi)
        {
            var pageMachine = new GhostscriptMachine();

            pageMachine.ReadPage(_pdfComic, _page.Number, dpi);

            if (string.IsNullOrEmpty(_page.Path))
            {
                _page.Name = pageMachine.GetReadPageString();
                _page.Path = Path.Combine(_pdfComic.OutputDirectory, _page.Name);
            }

            _page.Ping();

            Console.WriteLine($" {dpi} -> {_page.Width} x {_page.Height}");

            return _page.Height;
        }
    }
}
