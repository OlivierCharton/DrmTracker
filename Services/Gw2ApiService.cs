using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DrmTracker.Services
{
    public class Gw2ApiService
    {
        private readonly Gw2ApiManager _gw2ApiManager;
        private readonly Logger _logger;

        public Gw2ApiService(Gw2ApiManager gw2ApiManager, Logger logger)
        {
            _gw2ApiManager = gw2ApiManager;
            _logger = logger;
        }

        public async Task<string> GetAccountName()
        {
            if (_gw2ApiManager.HasPermissions(_gw2ApiManager.Permissions) == false)
            {
                _logger.Warn("Permissions not granted.");
                return string.Empty;
            }

            try
            {
                var account = await _gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
                return account?.Name;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error while getting account name : {ex.Message}");
                return null;
            }
        }

        public async Task<List<AccountAchievement>> GetAchievements(List<int> ids)
        {
            if (_gw2ApiManager.HasPermissions(_gw2ApiManager.Permissions) == false)
            {
                _logger.Warn("Permissions not granted.");
                return null;
            }

            try
            {
                var achievements = await _gw2ApiManager.Gw2ApiClient.V2.Account.Achievements.GetAsync();
                return achievements.Where(a => ids.Contains(a.Id)).ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error while getting achievements : {ex.Message}");
                return null;
            }
        }
    }
}