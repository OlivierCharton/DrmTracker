﻿using Blish_HUD;
using DrmTracker.Interfaces;
using DrmTracker.Services;
using Gw2Sharp.WebApi;
using System;

namespace DrmTracker.UI.Controls
{
    public class LoadingSpinner : Blish_HUD.Controls.LoadingSpinner, ILocalizable
    {
        private Func<string> _setLocalizedTooltip;

        public LoadingSpinner()
        {
            LocalizingService.LocaleChanged += UserLocale_SettingChanged;
            UserLocale_SettingChanged(null, null);
        }

        public Func<string> SetLocalizedTooltip
        {
            get => _setLocalizedTooltip;
            set
            {
                _setLocalizedTooltip = value;
                BasicTooltipText = value?.Invoke();
            }
        }

        public void UserLocale_SettingChanged(object sender, ValueChangedEventArgs<Locale> e)
        {
            if (SetLocalizedTooltip != null) BasicTooltipText = SetLocalizedTooltip?.Invoke();
        }

        protected override void DisposeControl()
        {
            base.DisposeControl();

            GameService.Overlay.UserLocale.SettingChanged -= UserLocale_SettingChanged;
        }
    }
}