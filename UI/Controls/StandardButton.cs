﻿using Blish_HUD;
using DrmTracker.Interfaces;
using DrmTracker.Services;
using Gw2Sharp.WebApi;
using System;

namespace DrmTracker.UI.Controls
{
    public class StandardButton : Blish_HUD.Controls.StandardButton, ILocalizable
    {
        private Func<string> _setLocalizedText;
        private Func<string> _setLocalizedTooltip;

        public StandardButton()
        {
            LocalizingService.LocaleChanged += UserLocale_SettingChanged;
            UserLocale_SettingChanged(null, null);
        }

        public Func<string> SetLocalizedText
        {
            get => _setLocalizedText;
            set
            {
                _setLocalizedText = value;
                Text = value?.Invoke();
            }
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
            if (SetLocalizedText != null) Text = SetLocalizedText?.Invoke();
            if (SetLocalizedTooltip != null) BasicTooltipText = SetLocalizedTooltip?.Invoke();
        }

        protected override void DisposeControl()
        {
            base.DisposeControl();

            GameService.Overlay.UserLocale.SettingChanged -= UserLocale_SettingChanged;
        }
    }
}