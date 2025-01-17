using System.Collections.Generic;

namespace DrmTracker.Domain
{
    public class ApiIds
    {
        public int Clear { get; set; }
        public int FullCM { get; set; }
        public int Factions { get; set; }

        public List<int> GetIds()
        {
            return
            [
                Clear, FullCM, Factions
            ];
        }
    }
}
