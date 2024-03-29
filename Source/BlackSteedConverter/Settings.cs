﻿using System.IO.Compression;

namespace BlackSteedConverter
{
    public sealed class Settings
    {
        // All properties with a public setter are read from settings file

        public static string AdbPath { get; set; }

        public static string CbzDir { get; set; }

        public static string TitlesDir {  get; set; }

        public static bool SaveCover { get; set; }

        public static CompressionLevel CompressionLevel { get; set; }

        public static int NumberOfThreads { get; set; }

        public static ParallelOptions ParallelOptions { get; private set; }

        public static void SetParallelOptions(ParallelOptions parallelOptions)
        {
            ParallelOptions = parallelOptions;
        }
    }
}
