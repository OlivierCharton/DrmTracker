using System.Collections.Generic;

namespace DrmTracker.Domain
{
    public class Drm
    {
        public int Map { get; set; }
        public ApiIds ApiIds { get; set; }
        public List<int> FactionsIds { get; set; }
    }
}
