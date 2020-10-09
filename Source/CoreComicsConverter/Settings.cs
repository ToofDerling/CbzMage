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

        public static string SevenZipPath { get; private set; }

        public static string GhostscriptPath { get; private set; }

        public static bool InitializeGhostscript()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var ghostscriptVersion = GhostscriptVersion.GetInstalledVersion();
                if (ghostscriptVersion == null)
                {
                    ProgressReporter.Error("No Ghostscript installation found!");
                    return false;
                }

                GhostscriptPath = ghostscriptVersion.Exe;
                ProgressReporter.Info($"{GhostscriptPath} [{ghostscriptVersion.Version}]");
            }
            else
            {
                GhostscriptPath = "gs";
            }

            return true;
        }

        public static bool InitializeSevenZip()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var sevenZipVersion = SevenZipVersion.GetInstalledVersion();
                if (sevenZipVersion == null)
                {
                    ProgressReporter.Error("No 7-Zip installation found!");
                    return false;
                }

                SevenZipPath = sevenZipVersion.Exe;
                ProgressReporter.Info($"{SevenZipPath} [{sevenZipVersion.Version}]");
            }
            else
            {
                GhostscriptPath = "7z";
            }

            return true;
        }
    }
}
