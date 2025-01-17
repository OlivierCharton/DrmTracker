using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using DrmTracker.Domain;
using DrmTracker.Services;
using DrmTracker.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;

namespace DrmTracker.UI.Views
{
    public class DrmTrackerWindow : StandardWindow
    {

        private readonly ModuleSettings _moduleSettings;
        private readonly BusinessService _businessService;

        private LoadingSpinner _loadingSpinner;
        private FlowPanel _tableContainer;

        private readonly List<Label> _labels = new();

        private List<Map> _maps;
        private List<Faction> _factions;
        private List<Drm> _drms;

        public DrmTrackerWindow(AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion,
            AsyncTexture2D cornerIconTexture, ModuleSettings moduleSettings, BusinessService businessService) : base(background, windowRegion, contentRegion)
        {
            Parent = GameService.Graphics.SpriteScreen;
            Title = "DrmTracker";
            Emblem = cornerIconTexture;
            Location = new Point(300, 300);
            SavesPosition = true;

            _moduleSettings = moduleSettings;
            _businessService = businessService;

            _factions = businessService.GetFactions();
            _maps = businessService.GetMaps();
            _drms = businessService.GetDrms();
        }

        public void BuildUi()
        {
            FlowPanel mainContainer = new()
            {
                Parent = this,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.Fill,
                ControlPadding = new(3, 3),
                CanScroll = true,
            };

            mainContainer.Resized += (s, e) =>
            {
                var newWidth = mainContainer.Width - 20;
                _tableContainer.Width = newWidth;
            };

            StandardButton button = new Controls.StandardButton()
            {
                SetLocalizedText = () => "strings.MainWindow_Button_Refresh_Label",
                SetLocalizedTooltip = () => "strings.MainWindow_Button_Refresh_Tooltip",
                Parent = mainContainer
            };
            button.Click += async (s, e) => await DrawLines();

            #region Notifications
            _tableContainer = new()
            {
                Parent = mainContainer,
                WidthSizingMode = SizingMode.Standard,
                HeightSizingMode = SizingMode.AutoSize,
                ShowBorder = true,
                OuterControlPadding = new(5),
                ControlPadding = new(5),
            };
            _tableContainer.ContentResized += TableContainer_ContentResized;

            AddHeaders(_tableContainer);

            #endregion Notifications

            #region Spinner
            _loadingSpinner = new LoadingSpinner()
            {
                Parent = _tableContainer,
                Size = new Point(29, 29),
                Visible = false,
            };
            #endregion Spinner
        }

        private void AddHeaders(FlowPanel container)
        {
            _labels.Add(UiUtils.CreateLabel("", "", container).label);
            _labels.Add(UiUtils.CreateLabel("Clear", "", container).label);
            _labels.Add(UiUtils.CreateLabel("CM", "", container).label);
            foreach (var faction in _factions)
            {
                _labels.Add(UiUtils.CreateLabel(faction.ShortName, faction.Name, container).label);
            }
        }

        private async Task DrawLines()
        {
            _loadingSpinner.Visible = true;

            var accountDrm = await _businessService.GetAccountDrm();

            //Lines
            foreach (var map in _maps)
            {
                var drmProgression = accountDrm?.FirstOrDefault(a => a.Map == map.Id).AccountAchievement;

                var lineLabel = UiUtils.CreateLabel(map.ShortName, map.Name, _tableContainer);

                var label = UiUtils.CreateLabel("", "", _tableContainer);
                label.panel.BackgroundColor = GetBackgroundColor(drmProgression?.Clear, "Clear");
                //label.label.Text = "2"; //TODO GERER RETOUR

                label = UiUtils.CreateLabel("", "", _tableContainer);
                label.panel.BackgroundColor = GetBackgroundColor(drmProgression?.FullCM, "CM");

                foreach (var faction in _factions)
                {
                    label = UiUtils.CreateLabel("", "", _tableContainer);
                    label.panel.BackgroundColor = GetBackgroundColorFaction(drmProgression?.Factions, map.Id, faction.Id);
                }
            }

            _loadingSpinner.Visible = false;
        }


        private Color GetBackgroundColor(Gw2Sharp.WebApi.V2.Models.AccountAchievement accountAchievement, string type)
        {
            //No progress
            if (accountAchievement == null)
            {
                return Color.Red;
            }

            if (type == "Clear")
            {
                return accountAchievement.Done ? Color.Green : Color.Red;
            }
            else if (type == "CM")
            {
                if (accountAchievement.Done)
                    return Color.Green;

                return accountAchievement.Current > 0 ? Color.Blue : Color.Red;
            }
            else
            {
                if (accountAchievement.Done)
                    return Color.Green;

                //TODO : tooltip ou autre sur factions manquantes
                return accountAchievement.Current > 0 ? Color.Blue : Color.Red;
            }
        }

        private Color GetBackgroundColorFaction(Gw2Sharp.WebApi.V2.Models.AccountAchievement accountAchievement, int mapId, int factionId)
        {
            //No progress
            if (accountAchievement == null)
            {
                return Color.Red;
            }

            var matchingDrm = _drms.FirstOrDefault(drm => drm.Map == mapId);
            if (!matchingDrm.FactionsIds.Contains(factionId))
                return Color.Black;

            if (accountAchievement.Done || accountAchievement.Bits.Contains(factionId))
            {
                return Color.Green;
            }

            return Color.Red;
        }

        private void TableContainer_ContentResized(object sender, RegionChangedEventArgs e)
        {
            if (_labels?.Count >= 0)
            {
                int columns = 11;
                var parent = _labels.FirstOrDefault()?.Parent as FlowPanel;
                int width = (parent?.ContentRegion.Width - (int)(parent?.OuterControlPadding.X ?? 0) - ((int)(parent?.ControlPadding.X ?? 0) * (columns - 1))) / columns ?? 100;

                foreach (var label in _labels)
                {
                    label.Width = width - 10;
                }
            }
        }
    }
}