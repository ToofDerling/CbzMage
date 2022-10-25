using CbzMage.Shared.Helpers;

namespace PdfConverter
{
    public class PdfSettings
    {
        public static Settings Settings => new();

        public void CreateSettings()
        {
            var settingsHelper = new SettingsHelper();
            settingsHelper.CreateSettings(nameof(PdfSettings), Settings);

            ConfigureSettings();
        }

        private void ConfigureSettings()
        {
            //MinimumDpi
            if (Settings.MinimumDpi <= 0)
            {
                Settings.MinimumDpi = 300;
            }

            //MinimumHeight
            if (Settings.MinimumHeight <= 0)
            {
                Settings.MinimumHeight = 1920;
            }

            //MaximumHeight
            if (Settings.MaximumHeight <= 0)
            {
                Settings.MaximumHeight = 3840;
            }

            //JpgQuality
            if (Settings.JpgQuality <= 0)
            {
                Settings.JpgQuality = 95;
            }
            
            //NumberOfThreads
            if (Settings.NumberOfThreads <= 0)
            {
                Settings.NumberOfThreads = Math.Max(1, (Environment.ProcessorCount / 2) - 2);
            }
            
            //BufferSize
            if (Settings.BufferSize <= 0)
            {
                Settings.BufferSize = 20000000;
            }

            //GhostscriptMinVersion
            if (Settings.GhostscriptMinVersion <= 0)
            {
                Settings.GhostscriptMinVersion = 10;
            }
        }
    }
}
