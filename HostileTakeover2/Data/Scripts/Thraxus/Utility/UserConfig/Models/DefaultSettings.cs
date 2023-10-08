using System.Text;
using HostileTakeover2.Thraxus.Utility.UserConfig.Types;
using VRageMath;

// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Models
{
    public class DefaultSettings
    {
        public string SettingsDescription =>
            $"\n\t\t{nameof(EntityDetectionRange)} default is {EntityDetectionRange.Default} [{EntityDetectionRange.Type}].  Value must be between {EntityDetectionRange.Min} and {EntityDetectionRange.Max}.  This controls how far out a grinder looks for a grid to highlight the blocks of." +
            $"\n\t\tScrapMassScalar default is 0.8f [Floating Point].  Value must be between 0.01f and 1.0f.  This is the ratio of scrap mass to component mass." +
            $"\n\t\tScrapProductionTimeScalar default is 0.75f [Floating Point].  Value must be between 0.01f and 100.0f.  This is the ratio of scrap production time to component production time." +
            $"\n\t\tScrapVolumeScalar default is 0.7f [Floating Point].  Value must be between 0.01f and 1.0f.  This is the ratio of scrap volume to component volume." +
            $"\n\t";

        // User set settings
        public Setting<bool> CapturePlayerBlocks = new Setting<bool>(false, true, true, true);
        public Setting<bool> MirrorEasyNpcTakeovers = new Setting<bool>(false, false, false, false);
        public Setting<bool> UseWeaponGroup = new Setting<bool>(false, true, true, true);
        public Setting<bool> UseMedicalGroup = new Setting<bool>(false, true, true, true);
        public Setting<bool> UseTrapGroup = new Setting<bool>(false, true, true, true);
        public Setting<bool> UseHighlights = new Setting<bool>(false, true, true, true);
        public Setting<double> EntityDetectionRange = new Setting<double>(100, 250, 150, 150);

        // Likely unused settings (delete if true)
        public Setting<bool> HighlightAllBlocks = new Setting<bool>(false, false, false, false);
        public Setting<bool> HighlightAllBlocksInGroup = new Setting<bool>(false, true, true, true);
        public Setting<bool> HighlightSingleNearestBlockInActiveGroup = new Setting<bool>(false, false, false, false);

        // Mod hardcoded settings
        public const int BlockAddTickDelay = 10;
        public const int EntityAddTickDelay = 10;
        public const int GrinderTickDelay = 10;
        public const int MinorTickDelay = 2;
        public const int RecheckGridInterval = Common.References.TicksPerMinute * 3;
        
        // HighlightSettings
        public int HighlightDuration = Common.References.TicksPerSecond * 10;
        public int HighlightPulseDuration = 120;
        public int EnabledThickness = 10;
        public int DisabledThickness = -1;
        public Color ControlColor = Color.DodgerBlue;
        public Color MedicalColor = Color.Red;
        public Color WeaponColor = Color.Purple;
        public Color TrapColor = Color.LightSeaGreen;
        
        public UserSettings CopyTo(UserSettings userSettings)
        {
            userSettings.EntityDetectionRange = EntityDetectionRange.ToString();
            return userSettings;
        }

        public StringBuilder PrintSettings()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0, -2}{1} Settings", " ", "Hostile Takeover");
            sb.AppendLine("__________________________________________________\n");
            sb.AppendFormat("{0, -4}[{1}] {2}\n\n", " ", EntityDetectionRange, nameof(EntityDetectionRange));
            return sb;
        }
    }
}