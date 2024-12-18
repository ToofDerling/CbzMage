using CbzMage.Shared.Extensions;
using CbzMage.Shared.Settings;

namespace PdfConverter
{
    public class PdfConvertSettings
    {
        public static Settings Settings => new();

        private readonly SharedSettings _settingsHelper = new();

        public void CreateSettings()
        {
            _settingsHelper.CreateSettings(nameof(PdfConvertSettings), Settings);

            ConfigureSettings();
        }

        private void ConfigureSettings()
        {
            const string poppler = "Poppler";
            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

            if (Directory.Exists(poppler))
            {
                var pdfToPngPath = Path.Combine(poppler, "pdftoppm");
                var pdfImagesPath = Path.Combine(poppler, "pdfimages");

                if (isWindows)
                {
                    pdfToPngPath = Path.ChangeExtension(pdfToPngPath, "exe");
                    pdfImagesPath = Path.ChangeExtension(pdfImagesPath, "exe");
                }

                if (File.Exists(pdfToPngPath))
                {
                    Settings.SetPdfToPngPath(pdfToPngPath);
                }
                if (File.Exists(pdfImagesPath))
                {
                    Settings.SetPdfImagesPath(pdfImagesPath);
                }
            }

            if (!isWindows)
            {
                if (Settings.PdfImagesPath == null)
                {
                    Settings.SetPdfImagesPath("pdfimages");
                }
                if (Settings.PdfToPngPath == null)
                {
                    Settings.SetPdfToPngPath("pdftopng");
                }
            }

            if (Settings.PdfToPngPath == null || Settings.PdfImagesPath == null)
            {
                throw new FileNotFoundException($"{poppler} tools missing.");
            }

            // CbzDir
            if (!string.IsNullOrWhiteSpace(Settings.CbzDir))
            {
                Settings.CbzDir.CreateDirIfNotExists();
            }

            // SaveCover/SaveCoverOnly
            Settings.SaveCoverOnly = Settings.SaveCoverOnly && Settings.SaveCover;

            // SaveCoverDir
            if (Settings.SaveCover && !string.IsNullOrWhiteSpace(Settings.SaveCoverDir))
            {
                Settings.SaveCoverDir.CreateDirIfNotExists();
            }
            else
            {
                Settings.SaveCoverDir = null;
            }

            // MinimumDpi
            if (Settings.MinimumDpi <= 0)
            {
                Settings.MinimumDpi = 300;
            }

            // MinimumHeight
            if (Settings.MinimumHeight <= 0)
            {
                Settings.MinimumHeight = 1920;
            }

            // MaximumHeight
            if (Settings.MaximumHeight <= 0)
            {
                Settings.MaximumHeight = 3840;
            }

            // JpgQuality
            if (Settings.JpgQuality <= 0)
            {
                Settings.JpgQuality = 93;
            }

            // NumberOfThreads
            Settings.NumberOfThreads = Settings.NumberOfThreads <= 0 ? Environment.ProcessorCount : Settings.NumberOfThreads;
        }
    }
}
