using Microsoft.Win32;

namespace PdfConverter.AppVersions
{
    public static class AppVersionManager
    {
        public static List<AppVersion> GetInstalledVersionsOf(App app)
        {
            var map = GetInstalledVersionsOf(new App[] { app });
            return map[app];
        }

        public static Dictionary<App, List<AppVersion>> GetInstalledVersionsOf(params App[] apps)
        {
            var appMap = new Dictionary<App, List<AppVersion>>();

            var x64 = Environment.Is64BitProcess;

            // 64 bit exe requires 64 bit process. 32 bit exe can run in both 32 bit and 64 bit process
            var hklms = new List<RegistryKey> { RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32) };
            if (x64)
            {
                hklms.Add(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64));
            }

            foreach (var app in apps)
            {
                switch (app)
                {
                    case App.Ghostscript:
                        appMap[App.Ghostscript] = GhostscriptVersion.GetGhostscriptVersions(hklms, x64);
                        break;
                    case App.SevenZip:
                        appMap[App.SevenZip] = SevenZipVersion.GetSevenZipVersions(hklms, x64);
                        break;
                }
            }

            hklms.ForEach(hklm => hklm.Dispose());

            return appMap;
        }
    }
}