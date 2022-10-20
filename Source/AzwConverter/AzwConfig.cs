using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AzwConverter
{
    public class AzwConfig
    {
        public static Settings Settings => new();

        public void CreateSettings()
        {
            if (!File.Exists("AzwSettings.json") && File.Exists("appsettings.json"))
            {
                File.Move("appsettings.json", "AzwSettings.json");
            }

            using IHost host = Host.CreateDefaultBuilder().ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();

                config.AddJsonFile("AzwSettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"AzwSettings.User.json", true, false);

                IConfigurationRoot configRoot = config.Build();
                configRoot.Bind(Settings);
            }).Build();

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

        public void ConfigureSettings()
        {
            //AzwDir
            if (string.IsNullOrWhiteSpace(Settings.AzwDir) || !Directory.Exists(Settings.AzwDir))
            {
                throw new Exception("Must configure AzwDir in appsettings.json");
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
            var convertedTitlesDirName = string.IsNullOrWhiteSpace(Settings.ConvertedTitlesDirName)
                ? defaultConvertedTitlesDirName
                : Settings.ConvertedTitlesDirName;

            Settings.SetConvertedTitlesDir(Path.Combine(Settings.TitlesDir, convertedTitlesDirName));
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
            var numThreads = maxThreads > 0
                ? maxThreads
                : Math.Min(Settings.NumberOfThreads, Environment.ProcessorCount);

            Settings.SetParallelOptions(new ParallelOptions { MaxDegreeOfParallelism = numThreads });
        }
    }
}
