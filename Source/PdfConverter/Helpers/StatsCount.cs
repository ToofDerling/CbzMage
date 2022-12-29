namespace PdfConverter.Helpers
{
    public sealed class StatsCount
    {
        private static volatile int _largestPng = 0;
        private static long _totalPngSize = 0;

        private static volatile int _largestJpg = 0;
        private static long _totalJpgSize = 0;

        private static volatile int _largestStreamRead = 0;
        private static volatile int _streamReadCount = 0;

        public static void AddStreamRead(int read)
        {
            _streamReadCount++;

            if (read > _largestStreamRead)
            {
                _largestStreamRead = read;
            }
        }

        private static volatile int _totalConversionTime = 0;
        private static volatile int _imageConversionCount = 0;
        private static volatile int _imageResizeCount = 0;

        public static void AddImageConversion(int ms, bool resize, int png, int jpg)
        {
            _imageConversionCount++;

            if (resize)
            {
                _imageResizeCount++;
            }

            _totalConversionTime += ms;

            _totalPngSize += png;

            if (png > _largestPng)
            {
                _largestPng = png;
            }

            _totalJpgSize += jpg;

            if (jpg > _largestJpg)
            { 
                _largestJpg = jpg;
            }
        }

        public static void ShowStats()
        {
            if (_streamReadCount > 0)
            {
                Console.WriteLine($"Stream reads: {_streamReadCount} Largest read: {_largestStreamRead} ({nameof(Settings.WriteBufferSize)}: {Settings.WriteBufferSize})");
            }

            if (_imageConversionCount > 0)
            {
                Console.WriteLine($"Image conversions: {_imageConversionCount} (resizes: {_imageResizeCount}) Average ms: {_totalConversionTime / _imageConversionCount}");
                Console.WriteLine($"Largest Png: {_largestPng} Average: {_totalPngSize / _imageConversionCount}");
                Console.WriteLine($"Largest Jpg: {_largestJpg} Average: {_totalJpgSize / _imageConversionCount} ({nameof(Settings.ImageBufferSize)}: {Settings.ImageBufferSize})");
            }
        }
    }
}
