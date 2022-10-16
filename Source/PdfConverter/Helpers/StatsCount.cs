namespace PdfConverter.Helpers
{
    public class StatsCount
    {
        private static int largestPng = 0;

        private static int pngCount = 0;

        private static long totalPngSize = 0;

        public static void AddPng(int png)
        {
            pngCount++;

            totalPngSize += png;

            if (png > largestPng)
            {
                largestPng = png;
            }
        }

        private static int largestJpg = 0;

        public static void AddJpg(int jpg)
        {
            if (jpg > largestJpg)
            {
                largestJpg = jpg;
            }
        }

        private static int largestRead = 0;

        public static void AddRead(int read)
        {
            if (read > largestRead)
            {
                largestRead = read;
            }
        }

        public static volatile int NewBuffers = 0;
        public static volatile int CachedBuffers = 0;

        public static volatile int NewPageMachines = 0;
        public static volatile int CachedPageMachines = 0;

        private static long magickTotalReadTime = 0;
        private static int magickReadCount = 0;
        public static void AddMagicReadTime(long ms)
        {
            magickReadCount++;
            magickTotalReadTime += ms;
        }

        private static long magickTotalWriteTime = 0;
        private static int magickWriteCount = 0;

        public static void AddMagicWriteTime(long ms)
        {
            magickWriteCount++;
            magickTotalWriteTime += ms;
        }

        public static void ShowStats()
        {
            if (largestPng > 0)
            {
                Console.WriteLine($"Largest png: {largestPng} Largest pipe read: {largestRead}");
                Console.WriteLine($"Png count: {pngCount} Average size: {totalPngSize / pngCount}");
            }

            if (largestJpg > 0)
            {
                Console.WriteLine($"Largest jpg: {largestJpg}");
            }

            if (NewBuffers > 0)
            {
                Console.WriteLine($"Cached/new buffers: {CachedBuffers}/{NewBuffers}");
            }

            if (NewPageMachines > 0)
            {
                Console.WriteLine($"Cached/new pagemachines: {CachedPageMachines}/{NewPageMachines}");
            }

            if (magickReadCount > 0)
            {
                Console.WriteLine($"Magick reads: {magickReadCount} Average ms: {magickTotalReadTime / magickReadCount}");
            }

            if (magickWriteCount > 0)
            {
                Console.WriteLine($"Magick writes: {magickWriteCount} Average ms: {magickTotalWriteTime / magickWriteCount}");
            }
        }
    }
}
