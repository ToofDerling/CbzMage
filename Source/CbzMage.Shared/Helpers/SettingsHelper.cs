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
                    .AddJsonFile($"{baseName}.{env.EnvironmentName}.json", true, true);

                var configRoot = config.Build();
                configRoot.Bind(settingsClass);
            }).Build();
        }
    }
}
