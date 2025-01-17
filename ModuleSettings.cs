using Blish_HUD.Settings;

namespace DrmTracker
{
    public class ModuleSettings
    {
        //public SettingEntry<bool> EnableAutoRetry { get; set; }

        public ModuleSettings(SettingCollection settings)
        {
            SettingCollection internalSettings = settings.AddSubCollection("Internal");

            //EnableAutoRetry = internalSettings.DefineSetting(nameof(EnableAutoRetry), true);
        }
    }
}
