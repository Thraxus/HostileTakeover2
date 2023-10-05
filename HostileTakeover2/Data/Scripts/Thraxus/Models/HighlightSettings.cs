using System;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Utility.UserConfig.Settings;
using VRageMath;

namespace HostileTakeover2.Thraxus.Models
{
    public class HighlightSettings : IReset
    {
        public string Name;
        public bool Enabled;
        public int Thickness = DefaultSettings.EnabledThickness;
        public int Duration = DefaultSettings.EnabledThickness;
        public Color Color;
        public long PlayerId;

        // I'm only here for the interface... below is unused
        public void Reset() { }
    }
}