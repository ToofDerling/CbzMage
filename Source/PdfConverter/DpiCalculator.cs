using ImageMagick;
using PdfConverter.PageMachines;

namespace PdfConverter
{
    public class DpiCalculator
    {
        private readonly PopplerRenderPageMachine _pageMachine;

        private readonly int _pageNumber;

        private readonly int _wantedImageWidth;

        private readonly Pdf _pdf;

        public DpiCalculator(PopplerRenderPageMachine pageMachine, Pdf pdf, int wantedImageWidth, int pageNumber)
        {
            _pageMachine = pageMachine;

            _wantedImageWidth = wantedImageWidth;

            _pdf = pdf;

            _pageNumber = pageNumber;
        }

        public int CalculateDpi()
        {
            var dpi = Settings.MinimumDpi;

            if (!TryGetImageWidth(dpi, out var width))
            {
                return dpi;
            }

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

                    if (!TryGetImageWidth(dpi, out nextWidth))
                    {
                        return dpi;
                    }
                }
            }
            else
            {
                var step = 5;
                dpi += step;

                if (!TryGetImageWidth(dpi, out var nextWidth))
                {
                    return dpi;
                }

                // Calculate big step if next width produced a large enough difference 
                if (nextWidth < wantedWidth && wantedWidth - nextWidth > 15)
                {
                    var nextDiff = wantedWidth - nextWidth;
                    var bigStep = CalculateBigStep(diff, nextDiff, step);

                    dpi = Settings.MinimumDpi + bigStep;

                    if (!TryGetImageWidth(dpi, out nextWidth))
                    {
                        return dpi;
                    }
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

                if (!TryGetImageWidth(dpi, out nextWidth))
                {
                    return dpi;
                }
            }

            while (nextWidth < wantedWidth)
            {
                dpi++;

                if (!TryGetImageWidth(dpi, out nextWidth))
                {
                    return dpi;
                }
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

        private readonly List<string> _errors = new();

        public List<string> GetErrors() => _errors;

        private bool TryGetImageWidth(int dpi, out int width)
        {
            using var runner = _pageMachine.RenderPage(_pdf, new List<int> { _pageNumber }, dpi);
            using var stream = runner.GetOutputStream();

            using var image = new MagickImage();
            image.Ping(stream);

            width = image.Width;
            var dpiHeight = image.Height;

            DpiCalculated?.Invoke(this, new DpiCalculatedEventArgs(dpi, width, dpiHeight));

            runner.WaitForExitCode();
            _errors.AddRange(runner.GetStandardErrorLines());

            // Hard cap at the maximum height
            return dpiHeight <= Settings.MaximumHeight;
        }

        public event EventHandler<DpiCalculatedEventArgs> DpiCalculated;
    }
}
