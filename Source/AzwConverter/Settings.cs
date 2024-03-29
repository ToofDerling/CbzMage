﻿using System.IO.Compression;

namespace AzwConverter
{
    public sealed class Settings
    {
        // All properties with a public setter are read from settings file

        public static string[] TrimPublishers { get; set; }

        public static string AzwDir { get; set; }

        public static string TitlesDir { get; set; }

        public static string AnalysisDir { get; set; }

        public static string ConvertedTitlesDirName { get; set; }

        public static string ConvertedTitlesDir { get; private set; }

        public static void SetConvertedTitlesDir(string dir)
        {
            ConvertedTitlesDir = dir;
        }

        public static string CbzDir { get; set; }

        public static bool CbzDirSetBySystem { get; private set; }

        public static void SetCbzDirSetBySystem()
        {
            CbzDirSetBySystem = true;
        }

        public static bool ConvertAllBookTypes { get; set; }

        public static bool SaveCover { get; set; }
        /// <summary>
        /// If this is true SaveCover is also true
        /// </summary>
        public static bool SaveCoverOnly { get; set; }
        public static string? SaveCoverDir { get; set; }

        public static int NumberOfThreads { get; set; }

        public static ParallelOptions ParallelOptions { get; private set; }

        public static void SetParallelOptions(ParallelOptions parallelOptions)
        {
            ParallelOptions = parallelOptions;
        }

        public static CompressionLevel CompressionLevel { get; set; }

        public static string NewTitleMarker { get; set; }
        public static string UpdatedTitleMarker { get; set; }

        public static string[] AllMarkers { get; private set; }

        public static void SetAllMarkers()
        {
            AllMarkers = new string[] { NewTitleMarker, UpdatedTitleMarker };
        }

        public static string ArchiveName => "archive.db";
    }
}
