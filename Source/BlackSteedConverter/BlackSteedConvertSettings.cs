using CbzMage.Shared.Extensions;
using CbzMage.Shared.Settings;

namespace BlackSteedConverter
{
    public class BlackSteedConvertSettings
    {
        public static Settings Settings => new();

        private readonly SharedSettings _settingsHelper = new();

        public void CreateSettings()
        {
            _settingsHelper.CreateSettings(nameof(BlackSteedConvertSettings), Settings);

            ConfigureSettings();
        }

        private void ConfigureSettings()
        {
            //CbzDir
            if (!string.IsNullOrWhiteSpace(Settings.CbzDir))
            {
                Settings.CbzDir.CreateDirIfNotExists();
            }

            Settings.NumberOfThreads = _settingsHelper.GetThreadCount(Settings.NumberOfThreads);
            Settings.SetParallelOptions(new ParallelOptions { MaxDegreeOfParallelism = Settings.NumberOfThreads });
        }
    }
}
