using Blish_HUD;
using Blish_HUD.Modules.Managers;
using DrmTracker.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadingSpinner = DrmTracker.UI.Controls.LoadingSpinner;

namespace DrmTracker.Services
{
    public class BusinessService
    {
        private readonly ModuleSettings _moduleSettings;
        private readonly ContentsManager _contentsManager;
        private readonly Gw2ApiService _gw2ApiService;
        private readonly Func<LoadingSpinner> _getSpinner;
        private readonly UI.Controls.CornerIcon _cornerIcon;
        private readonly Logger _logger;

        private string _accountName { get; set; }
        private Data _data;
        private List<DrmProgression> _accountDrm;


        public BusinessService(ModuleSettings moduleSettings, ContentsManager contentsManager, Gw2ApiService gw2ApiService, Func<LoadingSpinner> getSpinner, UI.Controls.CornerIcon cornerIcon, Logger logger)
        {
            _moduleSettings = moduleSettings;
            _contentsManager = contentsManager;
            _gw2ApiService = gw2ApiService;
            _getSpinner = getSpinner;
            _cornerIcon = cornerIcon;
            _logger = logger;
        }

        public void LoadData()
        {
            var file = _contentsManager.GetFileStream("data.json");
            using (StreamReader sr = new StreamReader(file))
            {
                string content = sr.ReadToEnd();
                _data = JsonConvert.DeserializeObject<Data>(content);
            }
        }

        public async Task RefreshBaseData()
        {
            _getSpinner?.Invoke()?.Show();

            //Get accountName
            await RefreshAccountName();

            //Get user drm progression
            await RefreshProgression();

            _getSpinner?.Invoke()?.Hide();
        }

        public async Task<List<DrmProgression>> GetAccountDrm(bool forceRefresh = false)
        {
            if (_accountDrm == null || forceRefresh)
            {
                await RefreshBaseData();
            }

            return _accountDrm;
        }

        public List<Faction> GetFactions()
        {
            return _data.Factions;
        }

        public List<Map> GetMaps()
        {
            return _data.Maps;
        }

        public List<Drm> GetDrms()
        {
            return _data.Drms;
        }

        private async Task<bool> RefreshAccountName()
        {
            _accountName = await _gw2ApiService.GetAccountName();

            _cornerIcon.UpdateWarningState(string.IsNullOrWhiteSpace(_accountName));

            return !string.IsNullOrWhiteSpace(_accountName);
        }

        private async Task RefreshProgression()
        {
            var mapApiIdsDict = _data.Drms.ToDictionary(drm => drm.Map, drm => drm.ApiIds);
            var allApiIds = mapApiIdsDict.Values.SelectMany(m => m.GetIds()).ToList();

            var accountProgression = await _gw2ApiService.GetAchievements(allApiIds);

            _accountDrm = mapApiIdsDict.Select(m => new DrmProgression
            {
                Map = m.Key,
                AccountAchievement = new DrmAchievements
                {
                    Clear = accountProgression.FirstOrDefault(ap => ap.Id == m.Value.Clear),
                    Factions = accountProgression.FirstOrDefault(ap => ap.Id == m.Value.Factions),
                    FullCM = accountProgression.FirstOrDefault(ap => ap.Id == m.Value.FullCM)
                }
            }).ToList();
        }
    }
}
