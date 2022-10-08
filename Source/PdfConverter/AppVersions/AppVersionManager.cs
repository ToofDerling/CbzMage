using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace CoreComicsConverter.AppVersions
{
    public static class AppVersionManager
    {
        public static Dictionary<App, AppVersion> GetInstalledVersionOf(params App[] apps)
        {
            var appMap = AddInstalledVersions(apps);

            var versionMap = new Dictionary<App, AppVersion>();

            foreach (var app in apps)
            {
                switch (app)
                {
                    case App.Ghostscript:
                        versionMap[app] = GhostscriptVersion.GetInstalledVersion(appMap[app]);
                        break;
                    case App.SevenZip:
                        versionMap[app] = SevenZipVersion.GetInstalledVersion(appMap[app]);
                        break;
                }
            }

            return versionMap;
        }

        private static Dictionary<App, Dictionary<Version, AppVersion>> AddInstalledVersions(params App[] apps)
        {
            var appMap = new Dictionary<App, Dictionary<Version, AppVersion>>();

            var x64 = Environment.Is64BitProcess;

            // 64 bit exe requires 64 bit process. 32 bit exe can run in both 32 bit and 64 bit process
            var hklms = new List<RegistryKey> { RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32) };
            if (x64)
            {
                hklms.Add(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64));
            }

            foreach (var app in apps)
            {
                appMap[app] = new Dictionary<Version, AppVersion>();

                switch (app)
                {
                    case App.Ghostscript:
                        GhostscriptVersion.AddGhostscriptVersions(hklms, appMap[app], x64);
                        break;
                    case App.SevenZip:
                        SevenZipVersion.AddSevenZipVersions(hklms, appMap[app], x64);
                        break;
                }
            }

            hklms.ForEach(hklm => hklm.Dispose());

            return appMap;
        }
    }
}
