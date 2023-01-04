using CbzMage.Shared.Extensions;
using CbzMage.Shared;

namespace BlackSteedConverter
{
    public class BlackSteedSettings
    {
        public static Settings Settings => new();

        private readonly SharedSettings _settingsHelper = new();

        public void CreateSettings()
        {
            _settingsHelper.CreateSettings(nameof(BlackSteedSettings), Settings);

            ConfigureSettings();
        }

        private void ConfigureSettings()
        {
            //CbzDir
            if (!string.IsNullOrWhiteSpace(Settings.CbzDir))
            {
                Settings.CbzDir.CreateDirIfNotExists();
            }
        }
    }
}
