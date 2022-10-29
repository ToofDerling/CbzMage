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
            if (Settings.GhostscriptReaderThreads <= 0)
            {
                var gsCores = (Environment.ProcessorCount / 2) * 0.7;

                var readerThreads = Convert.ToInt32(gsCores);
                Settings.GhostscriptReaderThreads = Math.Max(2, readerThreads);
            }
        }
    }
}
