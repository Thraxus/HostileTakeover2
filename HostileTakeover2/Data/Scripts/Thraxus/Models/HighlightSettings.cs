using HostileTakeover2.Thraxus.Common.Interfaces;
using VRageMath;

namespace HostileTakeover2.Thraxus.Models
{
    public class HighlightSettings : IReset
    {
        public string Name;
        public bool Enabled;
        public int LineThickness;
        public int PulseDuration;
        public Color Color;
        public long PlayerId;

        // I'm only here for the interface... below is unused
        public void Reset() { }
    }
}