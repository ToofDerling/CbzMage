using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using Microsoft.Win32;

namespace CoreComicsConverter.AppVersions
{
    public class SevenZipVersion
    {
        public static List<AppVersion> GetInstalledVersions()
        {
            var versionsMap = new Dictionary<Version, AppVersion>();

            using var hklm32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            var x64 = Environment.Is64BitProcess;

            // 64 bit exe requires 64 bit process. 32 bit exe can run in both 32 bit and 64 bit process
            if (x64)
            {
                AddSevenZipVersions(hklm32, versionsMap, x64);

                using var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                // 64 bit exe will overrule 32 bit exe with the same version.
                AddSevenZipVersions(hklm64, versionsMap, x64);
            }
            else
            {
                AddSevenZipVersions(hklm32, versionsMap, x64);
            }

            return versionsMap.Values.AsList();
        }

        private static void AddSevenZipVersions(RegistryKey hklm, Dictionary<Version, AppVersion> versionsMap, bool x64)
        {
            using var sevenZipKey = hklm.OpenSubKey("SOFTWARE\\7-Zip\\");
            if (sevenZipKey == null)
            {
                return;
            }

            try
            {
                var path = sevenZipKey.GetValue("Path", string.Empty) as string;

                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    path = sevenZipKey.GetValue(x64 ? "Path64" : "Path32", string.Empty) as string;

                    if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    {
                        return;
                    }
                }

                var exe = Path.Combine(path, "7z.exe");

                if (!File.Exists(exe))
                {
                    return;
                }

                var fileVersion = FileVersionInfo.GetVersionInfo(exe);

                var version = new Version(fileVersion.FileVersion);

                versionsMap[version] = new AppVersion(version, exe);
            }
            catch (Exception ex)
            {
                ProgressReporter.Warning(ex.TypeAndMessage());
            }
        }

        public static AppVersion GetInstalledVersion()
        {
            var sevenZipVersions = GetInstalledVersions();

            if (sevenZipVersions.Count == 0)
            {
                return null;
            }

            if (sevenZipVersions.Count > 1)
            {
                sevenZipVersions = sevenZipVersions.OrderByDescending(g => g.Version).AsList();
            }

            return sevenZipVersions.First();
        }
    }
}
