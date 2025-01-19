using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using DrmTracker.Domain;
using DrmTracker.Ressources;
using DrmTracker.Services;
using DrmTracker.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using FlowPanel = DrmTracker.UI.Controls.FlowPanel;

namespace DrmTracker.UI.Views
{
    public class DrmTrackerWindow : StandardWindow
    {
        private readonly ModuleSettings _moduleSettings;
        private readonly BusinessService _businessService;

        private LoadingSpinner _loadingSpinner;
        private FlowPanel _tableContainer;

        private readonly List<Label> _labels = new();
        private readonly List<StandardButton> _buttons = new();

        private List<Map> _maps;
        private List<Faction> _factions;
        private List<Drm> _drms;
        private List<DrmProgression> _accountDrms;

        private List<(Panel, Label)> _tablePanels = new();

        private ResourceManager _mapsResx;
        private ResourceManager _factionsResx;

        public DrmTrackerWindow(AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion,
            AsyncTexture2D cornerIconTexture, ModuleSettings moduleSettings, BusinessService businessService
           ) : base(background, windowRegion, contentRegion)
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

            _mapsResx = maps.ResourceManager;
            _factionsResx = factions.ResourceManager;
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

            #region Actions
            Controls.FlowPanel actionContainer = new()
            {
                Parent = mainContainer,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new(5),
                ControlPadding = new(5),
            };
            actionContainer.ContentResized += ActionContainer_ContentResized;

            StandardButton button;
            _buttons.Add(button = new Controls.StandardButton()
            {
                SetLocalizedText = () => strings.MainWindow_Button_Refresh_Label,
                Parent = actionContainer
            });
            button.Click += async (s, e) => await RefreshData();

            #endregion Actions

            #region Spinner
            _loadingSpinner = new LoadingSpinner()
            {
                Parent = actionContainer,
                Size = new Point(29, 29),
                Visible = false,
            };
            #endregion Spinner

            #region Legend
            Controls.FlowPanel legendContainer = new()
            {
                Parent = mainContainer,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new(5),
                ControlPadding = new(5),
            };

            DrawLegend(legendContainer);

            #endregion Legend

            DrawData();
        }

        public void InjectData(List<DrmProgression> accountDrms)
        {
            _accountDrms = accountDrms;

            DrawData();
        }

        private void AddHeaders(FlowPanel container)
        {
            _labels.Add(UiUtils.CreateLabel(() => "", () => "", container).label);
            _labels.Add(UiUtils.CreateLabel(() => "Clear", () => "", container).label);
            _labels.Add(UiUtils.CreateLabel(() => "CM", () => "", container).label);
            foreach (var faction in _factions)
            {
                _labels.Add(UiUtils.CreateLabel(() => _factionsResx.GetString($"{faction.Key}Label"), () => _factionsResx.GetString($"{faction.Key}Tooltip"), container).label);
            }
        }

        private void ClearLines()
        {
            for (int i = _tablePanels.Count - 1; i >= 0; i--)
            {
                _tablePanels.ElementAt(i).Item1.Dispose();
            }
        }

        private void DrawData()
        {
            ClearLines();

            if (_accountDrms == null)
            {
                DrawEmptyTable();
            }
            else
            {
                DrawLines();
            }
        }

        private void DrawEmptyTable()
        {
            var lineLabel = UiUtils.CreateLabel(() => strings.NoData, () => "", _tableContainer, alignment: HorizontalAlignment.Left, amount: 1);
            _tablePanels.Add(lineLabel);
        }

        private void DrawLines()
        {
            //Lines
            foreach (var map in _maps)
            {
                var drmProgression = _accountDrms?.FirstOrDefault(a => a.Map == map.Id)?.AccountAchievement;

                var lineLabel = UiUtils.CreateLabel(() => _mapsResx.GetString($"{map.Key}Label"), () => _mapsResx.GetString($"{map.Key}Tooltip"), _tableContainer, alignment: HorizontalAlignment.Left);
                if ((drmProgression?.HasFullSuccess).GetValueOrDefault())
                {
                    lineLabel.label.TextColor = Colors.Done;
                }
                _tablePanels.Add(lineLabel);

                var label = UiUtils.CreateLabel(() => "", () => "", _tableContainer);
                label.panel.BackgroundColor = GetBackgroundColor(drmProgression?.Clear, "Clear");
                _tablePanels.Add(label);

                label = UiUtils.CreateLabel(() => "", () => "", _tableContainer);
                label.panel.BackgroundColor = GetBackgroundColor(drmProgression?.FullCM, "CM");
                if (drmProgression?.FullCM == null || (drmProgression?.FullCM != null && !drmProgression.FullCM.Done))
                {
                    label.label.SetLocalizedText = () => $"{drmProgression.FullCM?.Current ?? 0} / {drmProgression.FullCM?.Max ?? 5}";
                }
                _tablePanels.Add(label);

                foreach (var faction in _factions)
                {
                    label = UiUtils.CreateLabel(() => "", () => "", _tableContainer);
                    label.panel.BackgroundColor = GetBackgroundColorFaction(drmProgression?.Factions, map.Id, faction.Id);
                    _tablePanels.Add(label);
                }
            }
        }

        private void DrawLegend(FlowPanel container)
        {
            var legend = UiUtils.CreateLabel(() => strings.Legend_Title, () => "", container, amount: 6);

            legend = UiUtils.CreateLabel(() => strings.Legend_None, () => "", container, amount: 6);
            legend.panel.BackgroundColor = Colors.None;

            legend = UiUtils.CreateLabel(() => strings.Legend_Todo, () => "", container, amount: 6);
            legend.panel.BackgroundColor = Colors.Todo;

            legend = UiUtils.CreateLabel(() => strings.Legend_Done, () => "", container, amount: 6);
            legend.panel.BackgroundColor = Colors.Done;
        }

        private async Task RefreshData()
        {
            _loadingSpinner.Visible = true;

            _accountDrms = await _businessService.GetAccountDrm(true);

            DrawData();

            _loadingSpinner.Visible = false;
        }

        private Color GetBackgroundColor(Gw2Sharp.WebApi.V2.Models.AccountAchievement accountAchievement, string type)
        {
            //No progress
            if (accountAchievement == null)
            {
                return Colors.Todo;
            }

            if (type == "Clear")
            {
                return accountAchievement.Done ? Colors.Done : Colors.Todo;
            }
            else
            {
                if (accountAchievement.Done)
                    return Colors.Done;

                return Colors.Todo;
            }
        }

        private Color GetBackgroundColorFaction(Gw2Sharp.WebApi.V2.Models.AccountAchievement accountAchievement, int mapId, int factionId)
        {
            //No progress
            if (accountAchievement == null)
            {
                return Colors.Todo;
            }

            var matchingDrm = _drms.FirstOrDefault(drm => drm.Map == mapId);
            if (!matchingDrm.FactionsIds.Contains(factionId))
                return Colors.None;

            if (accountAchievement.Done || accountAchievement.Bits.Contains(factionId))
            {
                return Colors.Done;
            }

            return Colors.Todo;
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

        private void ActionContainer_ContentResized(object sender, RegionChangedEventArgs e)
        {
            if (_buttons?.Count >= 0)
            {
                int columns = 4;
                var parent = _buttons.FirstOrDefault()?.Parent as FlowPanel;
                int width = (parent?.ContentRegion.Width - (int)parent.OuterControlPadding.X - ((int)parent.ControlPadding.X * (columns - 1))) / columns ?? 100;

                foreach (var button in _buttons)
                {
                    button.Width = width;
                }
            }
        }
    }
}