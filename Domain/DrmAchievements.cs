using Gw2Sharp.WebApi.V2.Models;

namespace DrmTracker.Domain
{
    public class DrmAchievements
    {
        public AccountAchievement Clear { get; set; }
        public AccountAchievement FullCM { get; set; }
        public AccountAchievement Factions { get; set; }

        public bool HasFullSuccess => 
            (Clear != null && Clear.Done) && 
            (FullCM != null && FullCM.Done) &&
            (Factions != null && Factions.Done);
    }
}
