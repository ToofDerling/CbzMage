//
// GhostscriptVersionInfo.cs
// This file is part of Ghostscript.NET library
//
// Author: Josip Habjan (habjan@gmail.com, http://www.linkedin.com/in/habjan) 
// Copyright (c) 2013-2016 by Josip Habjan. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using Microsoft.Win32;

namespace CoreComicsConverter.AppVersions
{
    public static class GhostscriptVersion
    {
        private static readonly Version MinVersion = new Version(9, 50);

        private static readonly Version MaxVersion = new Version(9, 52);

        private static readonly string[] hklmSubKeyNames = new[]
        {
            "SOFTWARE\\Artifex Ghostscript\\",
            "SOFTWARE\\GPL Ghostscript\\",
        };

        public static void AddGhostscriptVersions(List<RegistryKey> hklms, Dictionary<Version, AppVersion> versionsMap, bool x64)
        {
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

                                        versionsMap[version] = new AppVersion(version, exe);
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
        }

        public static AppVersion GetInstalledVersion(Dictionary<Version, AppVersion> versionsMap)
        {
            var ghostscriptVersions = versionsMap.Values.AsList();

            if (ghostscriptVersions.Count == 0)
            {
                return null;
            }

            if (ghostscriptVersions.Count > 1)
            {
                ghostscriptVersions = ghostscriptVersions.OrderByDescending(g => g.Version).AsList();
            }

            foreach (var ghostscriptVersion in ghostscriptVersions)
            {
                if ((MinVersion == null || ghostscriptVersion.Version >= MinVersion) && (MaxVersion == null || ghostscriptVersion.Version <= MaxVersion))
                {
                    return ghostscriptVersion;
                }
            }

            ProgressReporter.Warning($"No Ghostscript version >= {MinVersion} and =< {MaxVersion}");

            ghostscriptVersions.ForEach(gs => ProgressReporter.Warning($" Found {gs.Version} -> {gs.Exe}"));

            return null;
        }
    }
}
