using System.Diagnostics;

namespace PdfConverter.Helpers
{
    public sealed class DebugStatsCount
    {
        private static volatile int _largestWrittenCount = 0;
        private static long _totalBuffers = 0;

        private static volatile int _largestReadCount = 0;

        [Conditional("DEBUG")]
        public static void AddReadCount(int readCount)
        {
            if (readCount > _largestReadCount)
            {
                _largestReadCount = readCount;
            }
        }

        private static volatile int _totalImageConversionTime = 0;
        private static volatile int _imageConversionCount = 0;
        private static volatile int _imageResizedCount = 0;

        [Conditional("DEBUG")]
        public static void AddResize(bool resized)
        {
            if (resized)
            {
                _imageResizedCount++;
            }
        }

        [Conditional("DEBUG")]
        public static void AddWrittenCount(int ms, int writtenCount, bool resized)
        {
            AddResize(resized);

            _imageConversionCount++;

            _totalImageConversionTime += ms;

            _totalBuffers += writtenCount;

            if (writtenCount > _largestWrittenCount)
            {
                _largestWrittenCount = writtenCount;
            }
        }

        [Conditional("DEBUG")]
        public static void ShowStats()
        {
            if (_largestReadCount > 0)
            {
                Console.WriteLine($"Largest read: {_largestReadCount} (read request size: {Settings.ReadRequestSize})");
            }

            if (_imageConversionCount > 0)
            {
                Console.WriteLine($"Image conversions: {_imageConversionCount} (resizes: {_imageResizedCount}) Average ms: {_totalImageConversionTime / _imageConversionCount}");
                Console.WriteLine($"Largest write: {_largestWrittenCount} Average: {_totalBuffers / _imageConversionCount} (buffer size: {Settings.ImageBufferSize})");
            }
        }
    }
}
