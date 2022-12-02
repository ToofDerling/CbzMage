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
            if (settingsThreadCount <= 0)
            {
                var cores = (Environment.ProcessorCount / 2) * 0.7;

                var threadCount = Convert.ToInt32(cores);
                return Math.Max(2, threadCount);
            }

            return settingsThreadCount;
        }
    }
}
