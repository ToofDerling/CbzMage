using System.Diagnostics;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using Microsoft.Win32;

namespace PdfConverter.AppVersions
{
    public static class GhostscriptVersion
    {
        private static readonly string[] hklmSubKeyNames = new[]
        {
            "SOFTWARE\\Artifex Ghostscript\\",
            "SOFTWARE\\GPL Ghostscript\\",
        };

        public static List<AppVersion> GetGhostscriptVersions(List<RegistryKey> hklms, bool x64)
        {
            var versionList = new List<AppVersion>();

            foreach (var hklm in hklms)
            {
                foreach (var subKeyName in hklmSubKeyNames)
                {
                    using var ghostscriptKey = hklm.OpenSubKey(subKeyName);
                    if (ghostscriptKey == null)
                    {
                        continue;
                    }

                    // Each sub-key represents a version of the installed Ghostscript library
                    foreach (var versionKey in ghostscriptKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var ghostscriptVersion = ghostscriptKey.OpenSubKey(versionKey);

                            // Get the Ghostscript native library path
                            var gsDll = ghostscriptVersion.GetValue("GS_DLL", string.Empty) as string;

                            if (!string.IsNullOrEmpty(gsDll) && File.Exists(gsDll))
                            {
                                string exe = null;

                                // 64 bit exe requires 64 bit process. 
                                if (x64 && gsDll.EndsWith("gsdll64.dll"))
                                {
                                    exe = "gswin64c.exe";
                                }

                                // 32 bit exe can run in both 32 bit and 64 bit process
                                if (exe == null && gsDll.EndsWith("gsdll32.dll"))
                                {
                                    exe = "gswin32c.exe";
                                }

                                if (!string.IsNullOrEmpty(exe))
                                {
                                    var bin = Path.GetDirectoryName(gsDll);
                                    exe = Path.Combine(bin, exe);

                                    if (File.Exists(exe))
                                    {
                                        var fileVersion = FileVersionInfo.GetVersionInfo(exe);

                                        var version = new Version(fileVersion.FileVersion);

                                        versionList.Add(new AppVersion(exe, version));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ProgressReporter.Warning(ex.TypeAndMessage());
                        }
                    }
                }
            }

            return versionList;
        }
    }
}