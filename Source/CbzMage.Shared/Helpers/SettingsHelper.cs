using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CbzMage.Shared.Helpers
{
    public class SettingsHelper
    {
        public void CreateSettings(string baseName, object settingsClass)
        {
            using IHost host = Host.CreateDefaultBuilder().ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();

                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile($"{baseName}.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"{baseName}.User.json", true, false)
                    .AddJsonFile($"{baseName}.{env.EnvironmentName}.json", true, false);

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
                var threadCountFraction = Environment.ProcessorCount / fraction;
                
                var calculatedThreadCount = Convert.ToInt32(threadCountFraction);

                calculatedThreadCount = Math.Min(calculatedThreadCount, maxThreads);
                calculatedThreadCount = Math.Max(calculatedThreadCount, minThreads);

                return calculatedThreadCount;
            }

            return settingsThreadCount;
        }
    }
}
