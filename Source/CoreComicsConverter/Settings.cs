using System;

namespace CoreComicsConverter
{
    public static class Settings
    {
        public static readonly int MinimumDpi = 300;

        public static readonly int JpegQuality = 98;

        public static readonly int ParallelThreads = Environment.ProcessorCount;

        //TODO detect
        public static readonly string GhostscriptWin = @"C:\Program Files\gs\gs9.52\bin\gswin64c.exe";

        public static readonly string GhostscriptUnix = "gs";

        public static readonly string GhostscriptPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? GhostscriptWin : GhostscriptUnix;

        public static readonly string SevenZipWin = @"C:\Program Files\7-Zip\7z.exe";

        public static readonly string SevenZipLinux = @"7z";

        public static readonly string SevenZipPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? SevenZipWin : SevenZipLinux;
    }
}
