using CoreComicsConverter.AppVersions;
using CoreComicsConverter.Helpers;
using System;

namespace CoreComicsConverter
{
    public static class Settings
    {
        public static readonly int MinimumDpi = 300;

        public static readonly int JpegQuality = 95;

        public static readonly int StandardHeight = 3075;

        public static readonly int MaximumHeight = 4150;

        public static readonly int ParallelThreads = Environment.ProcessorCount;

        //TODO detect
        public static readonly string SevenZipWin = @"C:\Program Files\7-Zip\7z.exe";

        public static readonly string SevenZipUnix = @"7z";

        public static readonly string SevenZipPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? SevenZipWin : SevenZipUnix;

        public static string GhostscriptPath { get; private set; }

        public static bool InitializeGhostscript()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var gsVersion = GhostscriptVersion.GetInstalledVersion();
                if (gsVersion == null)
                {
                    ProgressReporter.Error("No Ghostscript installation found!");
                    return false;
                }

                GhostscriptPath = gsVersion.Exe;
                ProgressReporter.Info(GhostscriptPath);
            }
            else
            {
                GhostscriptPath = "gs";
            }

            return true;
        }
    }
}
