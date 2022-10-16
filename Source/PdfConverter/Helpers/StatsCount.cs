using System;

namespace PdfConverter.Helpers
{
    public class StatsCount
    {
        public static int LargestPng = 0;

        public static void AddPng(int png)
        {
            if (png > LargestPng)
            {
                LargestPng = png;
            }
        }

        public static int LargestJpg = 0;

        public static void AddJpg(int jpg)
        {
            if (jpg > LargestJpg)
            {
                LargestJpg = jpg;
            }
        }

        public static int LargestRead = 0;

        public static void AddRead(int read)
        {
            if (read > LargestRead)
            {
                LargestRead = read;
            }
        }

        public static int NewBuffers = 0;
        public static int CachedBuffers = 0;

        public static int NewPageMachines = 0;
        public static int CachedPageMachines = 0;

        public static void ShowStats()
        {
            if (LargestPng > 0)
            {
                Console.WriteLine($"Largest png: {LargestPng} read: {LargestRead}");
            }

            if (LargestJpg > 0)
            {
                Console.WriteLine($"Largest jpg: {LargestJpg}");
            }

            if (NewBuffers > 0)
            {
                Console.WriteLine($"Cached/new buffers: {CachedBuffers}/{NewBuffers}");
            }

            if (NewPageMachines > 0)
            {
                Console.WriteLine($"Cached/new page machines: {CachedPageMachines}/{NewPageMachines}");
            }
        }
    }
}
