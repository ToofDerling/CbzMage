namespace PdfConverter.Ghostscript
{
    internal class PipeHelper
    {
        private static readonly bool _isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static string GetPipeName()
        {
            if (_isWindows)
            {
                return $"CbzMage-{Guid.NewGuid()}";
            }

            return $"/tmp/CbzMage-{Guid.NewGuid()}";
        }

        public static string GetPipePath(string pipeName)
        {
            if (_isWindows)
            {
                return $"\\\\.\\pipe\\{pipeName}";
            }

            return pipeName;
        }
    }
}
