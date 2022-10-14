using CbzMage.Shared.Helpers;
using PdfConverter.AppVersions;
using System;

namespace PdfConverter
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

        public static bool Initialize(params App[] apps)
        {
            var ok = true;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var versionMap = AppVersionManager.GetInstalledVersionOf(apps);

                foreach (var key in versionMap)
                {
                    var app = key.Key;
                    var appVersion = key.Value;

                    switch (app)
                    {
                        case App.Ghostscript:
                            if (appVersion == null)
                            {
                                ok = false;
                                ProgressReporter.Error("No Ghostscript installation found!");
                            }
                            else
                            {
                                GhostscriptPath = appVersion.Exe;
                                ProgressReporter.Info($"{GhostscriptPath} [{appVersion.Version}]");
                            }
                            break;
                        case App.SevenZip:
                            if (appVersion == null)
                            {
                                ok = false;
                                ProgressReporter.Error("No 7-Zip installation found!");
                            }
                            else
                            {
                                SevenZipPath = appVersion.Exe;
                                ProgressReporter.Info($"{SevenZipPath} [{appVersion.Version}]");
                            }
                            break;
                    }
                }

                if (ok)
                {
                    Console.WriteLine();
                }
            }
            else
            {
                GhostscriptPath = "gs";
                SevenZipPath = "7z";
            }

            return ok;
        }
    }
}
