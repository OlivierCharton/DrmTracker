using System.Collections.Generic;

namespace DrmTracker.Domain
{
    public class Data
    {
        public List<Map> Maps { get; set; }
        public List<Faction> Factions { get; set; }
        public List<Drm> Drms { get; set; }
    }
}
