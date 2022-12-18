using System.Diagnostics;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using Microsoft.Win32;

namespace PdfConverter.AppVersions
{
    public static class SevenZipVersion
    {
        public static List<AppVersion> GetSevenZipVersions(List<RegistryKey> hklms, bool x64)
        {
            var versionList = new List<AppVersion>();

            foreach (var hklm in hklms)
            {
                using var sevenZipKey = hklm.OpenSubKey("SOFTWARE\\7-Zip\\");
                if (sevenZipKey == null)
                {
                    continue;
                }

                try
                {
                    var path = sevenZipKey.GetValue("Path", string.Empty) as string;

                    if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    {
                        path = sevenZipKey.GetValue(x64 ? "Path64" : "Path32", string.Empty) as string;

                        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                        {
                            return versionList;
                        }
                    }

                    var exe = Path.Combine(path, "7z.exe");

                    if (!File.Exists(exe))
                    {
                        return versionList;
                    }

                    var fileVersion = FileVersionInfo.GetVersionInfo(exe);

                    var version = new Version(fileVersion.FileVersion);

                    versionList.Add(new AppVersion(exe, version));
                }
                catch (Exception ex)
                {
                    ProgressReporter.Warning(ex.TypeAndMessage());
                }
            }

            return versionList;
        }

        public static AppVersion GetInstalledVersion(Dictionary<Version, AppVersion> versionsMap)
        {
            var sevenZipVersions = versionsMap.Values.AsList();

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