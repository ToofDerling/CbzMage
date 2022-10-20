namespace PdfConverter.Helpers
{
    public class StatsCount
    {
        private static volatile int largestPng = 0;
        private static long totalPngSize = 0;

        private static volatile int largestJpg = 0;
        private static long totalJpgSize = 0;

        private static volatile int largestPipeRead = 0;
        private static volatile int pipeReadCount = 0;

        public static void AddPipeRead(int read)
        {
            pipeReadCount++;

            if (read > largestPipeRead)
            {
                largestPipeRead = read;
            }
        }

        private static volatile int newBuffers = 0;
        private static volatile int cachedBuffers = 0;

        public static void AddBuffer(bool cached)
        {
            if (cached)
            {
                cachedBuffers++;
            }
            else
            {
                newBuffers++;
            }
        }

        private static volatile int newPageMachines = 0;
        private static volatile int cachedPageMachines = 0;

        public static void AddPageMachine(bool cached)
        {
            if (cached)
            {
                cachedPageMachines++;
            }
            else
            {
                newPageMachines++;
            }
        }

        private static volatile int magickTotalReadTime = 0;
        private static volatile int magickReadCount = 0;
        private static volatile int magickResizeCount = 0;

        public static void AddMagickRead(int ms, bool resize, int png)
        {
            magickReadCount++;

            if (resize)
            {
                magickResizeCount++;
            }

            magickTotalReadTime += ms;

            totalPngSize += png;

            if (png > largestPng)
            {
                largestPng = png;
            }
        }

        private static volatile int magickTotalWriteTime = 0;
        private static volatile int magickWriteCount = 0;

        public static void AddMagickWrite(int ms, int jpg)
        {
            magickWriteCount++;
            magickTotalWriteTime += ms;

            totalJpgSize += jpg;

            if (jpg > largestJpg)
            {
                largestJpg = jpg;
            }
        }

        private static volatile int magickRetryCount = 0;
        private static volatile int magickTotalReadWriteTime = 0;
        private static volatile int magickReadWriteCount = 0;

        public static void AddMagickReadWrite(int ms, int retries, bool resize)
        {
            magickReadWriteCount++;
            magickTotalReadWriteTime += ms;

            magickRetryCount += retries;
            if (resize)
            {
                magickResizeCount++;
            }
        }

        public static void ShowStats()
        {
            if (Settings.Mode == Mode.Slower)
            {
                if (magickReadWriteCount > 0)
                {
                    Console.WriteLine($"Magick read/writes {magickReadWriteCount} (retries: {magickRetryCount} / resizes: {magickResizeCount}) Average ms {magickTotalReadWriteTime / magickReadWriteCount}");
                }

                return;
            }

            if (pipeReadCount > 0)
            {
                Console.WriteLine($"Pipe reads: {pipeReadCount} Largest read: {largestPipeRead}");
            }

            if (magickReadCount > 0)
            {
                Console.WriteLine($"Magick reads: {magickReadCount} (resizes: {magickResizeCount}) Average ms: {magickTotalReadTime / magickReadCount}");
            }

            if (magickWriteCount > 0)
            {
                Console.WriteLine($"Magick writes: {magickWriteCount} Average ms: {magickTotalWriteTime / magickWriteCount}");
            }

            if (magickReadCount > 0 && largestPng > 0)
            {
                Console.WriteLine($"Largest Png: {largestPng} Average: {totalPngSize / magickReadCount}");
            }

            if (magickWriteCount > 0 && largestJpg > 0)
            {
                Console.WriteLine($"Largest Jpg: {largestJpg} Average: {totalJpgSize / magickWriteCount}");
            }

            if (newBuffers > 0)
            {
                Console.WriteLine($"Cached/new buffers: {cachedBuffers}/{newBuffers}");
            }

            if (newPageMachines > 0)
            {
                Console.WriteLine($"Cached/new pagemachines: {cachedPageMachines}/{newPageMachines}");
            }
        }
    }
}
