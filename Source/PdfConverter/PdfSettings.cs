using CbzMage.Shared.Helpers;

namespace PdfConverter
{
    public class PdfSettings
    {
        public static Settings Settings => new();

        private readonly SettingsHelper _settingsHelper = new();

        public void CreateSettings()
        {
            _settingsHelper.CreateSettings(nameof(PdfSettings), Settings);

            ConfigureSettings();
        }

        private void ConfigureSettings()
        {
            Settings.GhostscriptPath = @"C:\Program Files\gs\gs10.00.0\bin\gswin64c.exe";

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
            Settings.GhostscriptReaderThreads =
                _settingsHelper.GetThreadCount(Settings.GhostscriptReaderThreads); 
        }
    }
}
