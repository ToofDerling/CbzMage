using System;

namespace CoreComicsConverter
{
    public static class Settings
    {
        public static readonly int MinimumDpi = 300;

        public static readonly int JpegQuality = 98;

        public static readonly int ParallelThreads = Environment.ProcessorCount;

        public static readonly string GhostscriptPath = @"C:\Program Files\gs\gs9.52\bin\gswin64c.exe";
    }
}
