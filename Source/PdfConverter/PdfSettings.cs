using CbzMage.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (Settings.MinimumHeight <= 0)
            {
                Settings.MinimumHeight = 1920;
            }

            //JpegQuality
            if (Settings.JpegQuality <= 0)
            {
                Settings.JpegQuality = 95;
            }

            //PdfConverterMode



            //NumberOfThreads
            if (Settings.NumberOfThreads <= 0)
            {
                Settings.NumberOfThreads = Math.Max(1, (Environment.ProcessorCount / 2) - 2);
            }
        }

    }
}
