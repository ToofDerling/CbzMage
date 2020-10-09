using System;

namespace CoreComicsConverter.AppVersions
{
    public class AppVersion
    {
        public AppVersion(Version version, string gsExe)
        {
            Version = version;

            Exe = gsExe;
        }
  
        public Version Version { get; }

        public string Exe { get; }
    }
}
