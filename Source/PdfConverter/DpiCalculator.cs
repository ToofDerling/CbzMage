using ImageMagick;
using PdfConverter.Ghostscript;

namespace PdfConverter
{
    public class DpiCalculator
    {
        private readonly GhostscriptPageMachine _ghostScriptPageMachine;

        private readonly int _wantedImageWidth;

        private readonly Pdf _pdf;

        public DpiCalculator(GhostscriptPageMachine ghostScriptPageMachine, Pdf pdf, int wantedImageWidth)
        {
            _ghostScriptPageMachine = ghostScriptPageMachine;

            _wantedImageWidth = wantedImageWidth;

            _pdf = pdf;
        }

        public int CalculateDpi()
        {
            var dpi = Program.Settings.MinimumDpi;

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

                    dpi = Program.Settings.MinimumDpi + bigStep;
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
            var imageHandler = new GetSinglePipedImageDataHandler();

            _ghostScriptPageMachine.ReadPageList(_pdf, new List<int> { 1 }, dpi, imageHandler);

            var buffer = imageHandler.WaitForImageDate();

            var image = new MagickImage();
            image.Ping(buffer.Buffer, 0, buffer.Count);

            buffer.Release();

            DpiCalculated?.Invoke(this, new DpiCalculatedEventArgs(dpi, Program.Settings.MinimumDpi, image.Width, image.Height));
            return image.Width;
        }

        public event EventHandler<DpiCalculatedEventArgs> DpiCalculated;

    }
}
