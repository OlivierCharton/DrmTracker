﻿using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.GameIntegration;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using DrmTracker.Domain;
using DrmTracker.Ressources;
using DrmTracker.Services;
using DrmTracker.UI.Controls;
using DrmTracker.UI.Views;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Controls = DrmTracker.UI.Controls;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DrmTracker
{
    [Export(typeof(Module))]
    public class DrmTracker : Module
    {
        private static readonly Logger Logger = Logger.GetLogger<DrmTracker>();

        public static ModuleSettings ModuleSettings { get; private set; }
        public static Gw2ApiService Gw2ApiService { get; private set; }
        public static BusinessService BusinessService { get; private set; }
        public static UI.Controls.CornerIcon CornerIcon { get; private set; }

        private bool _dataLoaded;
        private bool _shouldOpenWindow;
        private double _openWindowTimer = 0;

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        // Ideally you should keep the constructor as is.
        // Use <see cref="Initialize"/> to handle initializing the module.
        [ImportingConstructor]
        public DrmTracker([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            DrmTrackerInstance = this;
        }

        // Define the settings you would like to use in your module.  Settings are persistent
        // between updates to both Blish HUD and your module.
        protected override void DefineSettings(SettingCollection settings)
        {
            ModuleSettings = new ModuleSettings(settings);
        }

        // Allows your module to perform any initialization it needs before starting to run.
        // Please note that Initialize is NOT asynchronous and will block Blish HUD's update
        // and render loop, so be sure to not do anything here that takes too long.
        protected override void Initialize()
        {
            // SOTO Fix
            if (Program.OverlayVersion < new SemVer.Version(1, 1, 0))
            {
                try
                {
                    var tacoActive = typeof(TacOIntegration).GetProperty(nameof(TacOIntegration.TacOIsRunning)).GetSetMethod(true);
                    tacoActive?.Invoke(GameService.GameIntegration.TacO, new object[] { true });
                }
                catch { /* NOOP */ }
            }

            CornerIcon = new Controls.CornerIcon(ContentsManager)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Priority = 683537108
            };

            Gw2ApiService = new Gw2ApiService(Gw2ApiManager, Logger);
            BusinessService = new BusinessService(ModuleSettings, ContentsManager, Gw2ApiService, () => _apiSpinner, CornerIcon, Logger);

            Gw2ApiManager.SubtokenUpdated += OnApiSubTokenUpdated;

            GameService.Overlay.UserLocale.SettingChanged += OnLocaleChanged;
        }

        private async void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            await BusinessService.RefreshBaseData();

            var userDrms = await BusinessService.GetAccountDrm();
            // Load textures
            _emblemTexture = ContentsManager.GetTexture("emblem.png");
            _windowBackgroundTexture = AsyncTexture2D.FromAssetId(155985);

            _mainWindow = new(
                _windowBackgroundTexture,
                new Rectangle(40, 26, 913, 691),
                new Rectangle(60, 36, 893, 675),
                _emblemTexture,
                ModuleSettings,
                BusinessService,
                userDrms)
            {
                SavesPosition = true,
                Width = 1200,
                Height = 500,
            };
            _mainWindow.BuildUi();

            _dataLoaded = true;
            CornerIcon.UpdateWarningState(false);
        }

        private void OnLocaleChanged(object sender, ValueChangedEventArgs<Locale> eventArgs)
        {
            LocalizingService.OnLocaleChanged(sender, eventArgs);
        }

        protected override async Task LoadAsync()
        {
            //Load Config
            BusinessService.LoadData();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            HandleCornerIcon();

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void HandleCornerIcon()
        {
            CornerIcon.Click += delegate
            {
                if (_dataLoaded)
                    _mainWindow.ToggleWindow();
                else
                    _shouldOpenWindow = true;
            };

            _apiSpinner = new Controls.LoadingSpinner()
            {
                Location = new Point(CornerIcon.Left, CornerIcon.Bottom + 3),
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(CornerIcon.Width, CornerIcon.Height),
                SetLocalizedTooltip = () => strings.LoadingSpinner_Fetch,
                Visible = false
            };
        }

        protected override void Update(GameTime gameTime)
        {
            if (_shouldOpenWindow)
            {
                if (_openWindowTimer > 100)
                {
                    if (_dataLoaded)
                    {
                        _shouldOpenWindow = false;
                        _mainWindow.ToggleWindow();
                    }
                    _openWindowTimer = 0;
                }
                else
                {
                    _openWindowTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                }
            }
        }

        // For a good module experience, your module should clean up ANY and ALL entities
        // and controls that were created and added to either the World or SpriteScreen.
        // Be sure to remove any tabs added to the Director window, CornerIcons, etc.
        protected override void Unload()
        {
            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;

            CornerIcon?.Dispose();
            _apiSpinner?.Dispose();
            _mainWindow?.Dispose();
            _emblemTexture?.Dispose();

            // All static members must be manually unset
            // Static members are not automatically cleared and will keep a reference to your,
            // module unless manually unset.
            DrmTrackerInstance = null;
        }

        internal static DrmTracker DrmTrackerInstance;
        private AsyncTexture2D _windowBackgroundTexture;
        private Texture2D _emblemTexture;
        private UI.Controls.LoadingSpinner _apiSpinner;
        private DrmTrackerWindow _mainWindow;
    }
}