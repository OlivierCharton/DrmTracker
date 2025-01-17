using Blish_HUD;
using Gw2Sharp.WebApi;

namespace DrmTracker.Interfaces
{
    public interface ILocalizable
    {
        void UserLocale_SettingChanged(object sender, ValueChangedEventArgs<Locale> e);
    }
}