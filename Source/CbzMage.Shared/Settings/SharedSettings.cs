using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CbzMage.Shared.Settings
{
    public class SharedSettings
    {
        private const string _defaultSettings = "DefaultSettings";

        public void CreateSettings(string className, object settingsClass)
        {
            using IHost host = Host.CreateDefaultBuilder().ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();

                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile($"{_defaultSettings}.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"{className}.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"{className}.{env.EnvironmentName}.json", true, false);

                var configRoot = config.Build();
                configRoot.Bind(settingsClass);
            }).Build();
        }

        public int GetThreadCount(int settingsThreadCount)
        {
            const double fraction = 0.75;

            const int maxThreads = 8;
            const int minThreads = 2;

            if (settingsThreadCount <= 0)
            {
                var threadCountFraction = Environment.ProcessorCount * fraction;

                var calculatedThreadCount = Convert.ToInt32(threadCountFraction);

                calculatedThreadCount = Math.Min(calculatedThreadCount, maxThreads);
                calculatedThreadCount = Math.Max(calculatedThreadCount, minThreads);

                return calculatedThreadCount;
            }

            return settingsThreadCount;
        }

        public static string GetPageString(int pageNumber)
        {
            var page = pageNumber.ToString().PadLeft(4, '0');
            return $"page-{page}.jpg";
        }

        private static string ScanAllDirectoriesPattern => $"{Path.DirectorySeparatorChar}**";

        public static string GetDirectorySearchOption(string directory, out SearchOption searchOption)
        {
            searchOption = SearchOption.TopDirectoryOnly;

            if (directory.EndsWith(ScanAllDirectoriesPattern))
            {
                searchOption = SearchOption.AllDirectories;
                directory = directory.Replace(ScanAllDirectoriesPattern, null);
            }

            return directory;
        }
    }
}
