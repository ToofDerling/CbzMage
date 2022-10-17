﻿namespace PdfConverter.Helpers
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

        public static volatile int NewBuffers = 0;
        public static volatile int CachedBuffers = 0;

        public static volatile int NewPageMachines = 0;
        public static volatile int CachedPageMachines = 0;

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

        public static void ShowStats()
        {
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

            if (magickReadCount > 0)
            {
                Console.WriteLine($"Largest Png: {largestPng} Average size: {totalPngSize / magickReadCount}");
            }

            if (magickWriteCount > 0)
            {
                Console.WriteLine($"Largest Jpg: {largestJpg} Average size: {totalJpgSize / magickWriteCount}");
            }

            if (NewBuffers > 0)
            {
                Console.WriteLine($"Cached/new buffers: {CachedBuffers}/{NewBuffers}");
            }

            if (NewPageMachines > 0)
            {
                Console.WriteLine($"Cached/new pagemachines: {CachedPageMachines}/{NewPageMachines}");
            }
        }
    }
}
