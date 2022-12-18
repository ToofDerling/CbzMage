namespace CbzMage.Shared.AppVersions
{
    public class AppVersion
    {
        public AppVersion(string exe, Version version)
        {
            Exe = exe;

            Version = version;
        }

        public string Exe { get; }

        public Version Version { get; }
    }
}