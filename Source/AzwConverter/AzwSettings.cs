using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;

namespace AzwConverter
{
    public class AzwSettings
    {
        public static Settings Settings => new();

        public void CreateSettings()
        {
            var settingsHelper = new SettingsHelper();
            settingsHelper.CreateSettings(nameof(AzwSettings), Settings);

            ConfigureSettings();
        }

        // For testing. If >0 overrules the NumberOfThreads setting.
        private const int maxThreads = 0;

        // Defaults
        private const string defaultTitlesDir = "Titles";
        private const string defaultCbzDir = "Cbz Backups";

        private const string defaultConvertedTitlesDirName = "Converted Titles";
        private const string defaultNewTitleMarker = ".NEW";
        private const string defaultUpdateTitleMarker = ".UPDATED";

        private void ConfigureSettings()
        {
            //AzwDir
            if (string.IsNullOrWhiteSpace(Settings.AzwDir))
            {
                throw new Exception("Must configure AzwDir in AzwSettings.json or AzwSettings.User.json");
            }
            if (!Directory.Exists(Settings.AzwDir))
            {
                throw new Exception($"{nameof(Settings.AzwDir)} [{Settings.AzwDir}] does not exist");
            }

            //TitlesDir
            if (string.IsNullOrWhiteSpace(Settings.TitlesDir))
            {
                var dir = new DirectoryInfo(Settings.AzwDir).Parent;
                Settings.TitlesDir = Path.Combine(dir.FullName, defaultTitlesDir);
                Settings.TitlesDir.CreateDirIfNotExists();
            }

            //SaveCover/SaveCoverOnly
            Settings.SaveCoverOnly = Settings.SaveCoverOnly && Settings.SaveCover;

            //SaveCoverDir
            if (Settings.SaveCover && !string.IsNullOrWhiteSpace(Settings.SaveCoverDir))
            {
                Settings.SaveCoverDir.CreateDirIfNotExists();
            }

            //ConvertedTitlesDirName
            if (string.IsNullOrWhiteSpace(Settings.ConvertedTitlesDirName))
            {
                Settings.ConvertedTitlesDirName = defaultConvertedTitlesDirName;
            }
            Settings.SetConvertedTitlesDir(Path.Combine(Settings.TitlesDir, Settings.ConvertedTitlesDirName));
            Settings.ConvertedTitlesDir.CreateDirIfNotExists();

            //CbzDir
            if (string.IsNullOrWhiteSpace(Settings.CbzDir))
            {
                var dir = new DirectoryInfo(Settings.AzwDir).Parent;
                Settings.CbzDir = Path.Combine(dir.FullName, defaultCbzDir);
                Settings.CbzDir.CreateDirIfNotExists();
            }

            //NewTitleMarker/UpdatedTitleMarker
            Settings.NewTitleMarker = string.IsNullOrWhiteSpace(Settings.NewTitleMarker)
                ? defaultNewTitleMarker
                : Settings.NewTitleMarker;

            Settings.UpdatedTitleMarker = string.IsNullOrWhiteSpace(Settings.UpdatedTitleMarker)
                ? defaultUpdateTitleMarker
                : Settings.UpdatedTitleMarker;

            Settings.SetAllMarkers();

            //TrimPublishers
            Settings.TrimPublishers ??= Array.Empty<string>();

            //NumberOfThreads
            if (maxThreads > 0)
            {
                Settings.NumberOfThreads = maxThreads;
            }
            else if (Settings.NumberOfThreads <= 0)
            {
               Settings.NumberOfThreads = Math.Min(Environment.ProcessorCount, 3);
            }
            Settings.SetParallelOptions(new ParallelOptions { MaxDegreeOfParallelism = Settings.NumberOfThreads });
        }
    }
}
