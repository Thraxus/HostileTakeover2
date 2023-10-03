using System.Runtime.CompilerServices;
using System.Text;
using HostileTakeover2.Thraxus.Utility.UserConfig.Types;
using VRage.Game;
using VRageMath;

// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Settings
{
    public static class DefaultSettings
    {
        public static readonly string SettingsDescription = 
            $"\n\t\t{nameof(EntityDetectionRange)} default is {EntityDetectionRange.Default} [{EntityDetectionRange.Type}].  Value must be between {EntityDetectionRange.Min} and {EntityDetectionRange.Max}.  This controls how far out a grinder looks for a grid to highlight the blocks of." +
            $"\n\t\tScrapMassScalar default is 0.8f [Floating Point].  Value must be between 0.01f and 1.0f.  This is the ratio of scrap mass to component mass." +
            $"\n\t\tScrapProductionTimeScalar default is 0.75f [Floating Point].  Value must be between 0.01f and 100.0f.  This is the ratio of scrap production time to component production time." +
            $"\n\t\tScrapVolumeScalar default is 0.7f [Floating Point].  Value must be between 0.01f and 1.0f.  This is the ratio of scrap volume to component volume." +
            $"\n\t";

        // User set settings
        public static readonly Setting<bool> MirrorEasyNpcTakeovers = new Setting<bool>(false, false, false, false);
        public static readonly Setting<bool> UseWeaponGroup = new Setting<bool>(false, true, true, true);
        public static readonly Setting<bool> UseMedicalGroup = new Setting<bool>(false, true, true, true);
        public static readonly Setting<bool> UseTrapGroup = new Setting<bool>(false, true, true, true);
        public static readonly Setting<bool> UseHighlights = new Setting<bool>(false, true, true, true);
        public static readonly Setting<double> EntityDetectionRange = new Setting<double>(100, 250, 150, 150);

        // Likely unused settings (delete if true)
        public static readonly Setting<bool> HighlightAllBlocks = new Setting<bool>(false, false, false, false);
        public static readonly Setting<bool> HighlightAllBlocksInGroup = new Setting<bool>(false, true, true, true);
        public static readonly Setting<bool> HighlightSingleNearestBlockInActiveGroup = new Setting<bool>(false, false, false, false);

        // Mod hardcoded settings
        public const int EntityAddTickDelay = 10;
        public const int BlockAddTickDelay = 10;
        public const int GrinderTickDelay = 10;
        public const int RecheckGridInterval = Common.References.TicksPerMinute * 3;

        // HighlightSettings
        public static long HighlightDuration = Common.References.TicksPerSecond * 60;
        public static int HighlightPulseDuration = 120;
        public static int EnabledThickness = 10;
        public static int DisabledThickness = -1;
        public static Color ControlColor = Color.DodgerBlue;
        public static Color MedicalColor = Color.Red;
        public static Color WeaponColor = Color.Purple;
        public static Color TrapColor = Color.LightSeaGreen;
        

        public static UserSettings CopyTo(UserSettings userSettings)
        {
            userSettings.EntityDetectionRange = EntityDetectionRange.ToString();
            return userSettings;
        }

        public static StringBuilder PrintSettings()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0, -2}{1} Settings", " ", "Hostile Takeover");
            sb.AppendLine("__________________________________________________\n");
            sb.AppendFormat("{0, -4}[{1}] {2}\n\n", " ", EntityDetectionRange, nameof(EntityDetectionRange));
            return sb;
        }
    }
}