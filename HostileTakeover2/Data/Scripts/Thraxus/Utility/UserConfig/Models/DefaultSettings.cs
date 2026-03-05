using System.Collections.Generic;
using System.Text;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Settings;
using VRageMath;

// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Models
{
    /// <summary>
    /// Holds all runtime-active settings for the mod.  Each user-configurable value is
    /// wrapped in a <see cref="UserSetting{T}"/> that carries the default, minimum, maximum,
    /// and current value together.  Hard-coded tick delays and visual constants are
    /// stored as plain fields or <c>const</c> values because they are not user-editable.
    ///
    /// <see cref="CopyTo"/> is used on first run to seed a blank <see cref="UserSettings"/>
    /// object with the correct default strings before writing it to disk.
    /// </summary>
    public class DefaultSettings
    {
        /// <summary>
        /// Human-readable description of every setting appended to the XML file so users
        /// know what each field does without consulting external documentation.
        /// <paramref name="validCategoryNames"/> must be supplied by the caller (e.g. from
        /// <see cref="LogCategory.GetRegisteredNames()"/>) rather than called inline here,
        /// because this method may be invoked before <c>DebugType.Initialize()</c> has run
        /// and the <see cref="LogCategory"/> registry is fully populated.
        /// </summary>
        public string GetSettingsDescription(string validCategoryNames) =>
            $"\n\t\t{nameof(EntityDetectionRange)} default is {EntityDetectionRange.Default} [{EntityDetectionRange.Type}].  Value must be between {EntityDetectionRange.Min} and {EntityDetectionRange.Max}.  Radius in metres used when searching for nearby NPC grids from the grinder position." +
            $"\n\t\t{nameof(AllowPlayerHacking)} default is {AllowPlayerHacking.Default} [{AllowPlayerHacking.Type}].  When true, players can hack NPC blocks (grind down and repair to claim ownership).  When false, the NPC always reclaims any block whose ownership changes." +
            $"\n\t\t{nameof(MirrorEasyNpcTakeovers)} default is {MirrorEasyNpcTakeovers.Default} [{MirrorEasyNpcTakeovers.Type}].  When true, forces UseHighlights, UseWeaponGroup, UseMedicalGroup, UseTrapGroup, and AllowPlayerHacking all off to mirror EasyNpcTakeovers behaviour." +
            $"\n\t\t{nameof(UseHighlights)} default is {UseHighlights.Default} [{UseHighlights.Type}].  Master toggle for the visual block highlight system.  All highlight sub-settings are ignored when this is false." +
            $"\n\t\t{nameof(HighlightAllGridsInRange)} default is {HighlightAllGridsInRange.Default} [{HighlightAllGridsInRange.Type}].  When true, every NPC-owned grid within detection range is highlighted simultaneously.  When false (default), only the single nearest NPC grid is highlighted." +
            $"\n\t\t{nameof(UseWeaponGroup)} default is {UseWeaponGroup.Default} [{UseWeaponGroup.Type}].  When true, turrets and weapon blocks are included as a highlight priority group." +
            $"\n\t\t{nameof(UseMedicalGroup)} default is {UseMedicalGroup.Default} [{UseMedicalGroup.Type}].  When true, medical, cryo, and survival-kit blocks are included as a highlight priority group." +
            $"\n\t\t{nameof(UseTrapGroup)} default is {UseTrapGroup.Default} [{UseTrapGroup.Type}].  When true, warhead and trap blocks are included as a highlight priority group." +
            $"\n\t\t{nameof(HighlightAllBlocks)} default is {HighlightAllBlocks.Default} [{HighlightAllBlocks.Type}].  When true, every block on the grid is highlighted rather than only important blocks." +
            $"\n\t\t{nameof(HighlightSingleNearestBlock)} default is {HighlightSingleNearestBlock.Default} [{HighlightSingleNearestBlock.Type}].  When true, only the single nearest important block (across all priority groups) is highlighted." +
            $"\n\t\t{nameof(HighlightSingleNearestBlockInActiveGroup)} default is {HighlightSingleNearestBlockInActiveGroup.Default} [{HighlightSingleNearestBlockInActiveGroup.Type}].  When true, only the single nearest block within the active priority group is highlighted." +
            $"\n\t\t{nameof(UseGrinderTierHighlighting)} default is {UseGrinderTierHighlighting.Default} [{UseGrinderTierHighlighting.Type}].  When true, the tier of the grinder limits how many blocks are highlighted in the default all-in-active-group mode.  Tier 4 (Elite) always shows all.  See BlocksPerGrinderTier for the per-tier block count.  Nearest blocks are shown first.  Ignored when any single-block or all-blocks override is active." +
            $"\n\t\t{nameof(BlocksPerGrinderTier)} default is {BlocksPerGrinderTier.Default} [{BlocksPerGrinderTier.Type}].  Number of blocks highlighted per grinder tier when UseGrinderTierHighlighting is active.  Tier N shows N x value blocks; tier 4 (Elite) always shows all regardless.  Value must be between {BlocksPerGrinderTier.Min} and {BlocksPerGrinderTier.Max}." +
            $"\n\t\t{nameof(UnknownGrinderTierBlockCount)} default is {UnknownGrinderTierBlockCount.Default} [{UnknownGrinderTierBlockCount.Type}].  Blocks shown for unrecognised grinder subtypes (modded grinders) when UseGrinderTierHighlighting is active.  0 = show all blocks.  Value must be between {UnknownGrinderTierBlockCount.Min} and {UnknownGrinderTierBlockCount.Max}." +
            $"\n\t\t{nameof(DebugMode)} default is {DebugMode.Default} [{DebugMode.Type}].  When true, extra diagnostic log messages and GPS markers are emitted to help identify issues during testing.  Disable before publishing." +
            $"\n\t\t{nameof(VerboseMode)} default is {VerboseMode.Default} [{VerboseMode.Type}].  When true, also logs high-frequency internal events (pool ops, per-grid init steps, topology fan-out).  Enabling VerboseMode implies DebugMode.  Disable before publishing." +
            $"\n\t\t{nameof(ActiveDebugCategories)} default is All [{nameof(LogCategory)}].  Comma-separated list of subsystems to log when DebugMode is active.  Valid values: {validCategoryNames}.  VerboseMode always enables all categories." +
            $"\n\t";

        // ── User-configurable settings ──────────────────────────────────────────────
        // Each Setting<T>(min, max, default, current) wraps the value with its valid range.

        /// <summary>When true, players can hack NPC blocks (grind down and repair to claim ownership). When false, the NPC always reclaims any block whose ownership changes.</summary>
        public UserSetting<bool> AllowPlayerHacking = new UserSetting<bool>(false, true, false, false);
        /// <summary>Whether the mod should mirror EasyNpcTakeovers behaviour when that mod is installed.</summary>
        public UserSetting<bool> MirrorEasyNpcTakeovers = new UserSetting<bool>(false, true, false, false);
        /// <summary>Master toggle for the visual highlight system.</summary>
        public UserSetting<bool> UseHighlights = new UserSetting<bool>(false, true, true, true);
        /// <summary>When true, every NPC-owned grid in detection range is highlighted simultaneously.  When false, only the nearest NPC grid is highlighted.</summary>
        public UserSetting<bool> HighlightAllGridsInRange = new UserSetting<bool>(false, true, false, false);
        /// <summary>Whether weapon blocks (turrets, modded weapons) are included in the highlight group.</summary>
        public UserSetting<bool> UseWeaponGroup = new UserSetting<bool>(false, true, true, true);
        /// <summary>Whether medical / cryo / survival-kit blocks are included in the highlight group.</summary>
        public UserSetting<bool> UseMedicalGroup = new UserSetting<bool>(false, true, true, true);
        /// <summary>Whether warhead blocks are included in the highlight group.</summary>
        public UserSetting<bool> UseTrapGroup = new UserSetting<bool>(false, true, true, true);
        /// <summary>Whether all blocks (not just important ones) should be highlighted.</summary>
        public UserSetting<bool> HighlightAllBlocks = new UserSetting<bool>(false, true, false, false);
        /// <summary>Whether only the single nearest important block (across all priority groups) should be highlighted.</summary>
        public UserSetting<bool> HighlightSingleNearestBlock = new UserSetting<bool>(false, true, false, false);
        /// <summary>Whether only the single nearest block within the active priority group should be highlighted.</summary>
        public UserSetting<bool> HighlightSingleNearestBlockInActiveGroup = new UserSetting<bool>(false, true, false, false);
        /// <summary>When true, the tier of the grinder limits how many blocks are highlighted in default mode; tier 4 (Elite) always shows all.</summary>
        public UserSetting<bool> UseGrinderTierHighlighting = new UserSetting<bool>(false, true, false, false);
        /// <summary>Number of blocks highlighted per grinder tier when UseGrinderTierHighlighting is active. Tier N shows N × value blocks; tier 4 (Elite) always shows all regardless.</summary>
        public UserSetting<int> BlocksPerGrinderTier = new UserSetting<int>(1, 10, 1, 1);
        /// <summary>Blocks shown for unrecognised grinder subtypes (modded grinders) when UseGrinderTierHighlighting is active. 0 = show all.</summary>
        public UserSetting<int> UnknownGrinderTierBlockCount = new UserSetting<int>(0, 10, 0, 0);
        /// <summary>Sphere radius (in metres) used when searching for nearby NPC grids from a grinder position.</summary>
        public UserSetting<double> EntityDetectionRange = new UserSetting<double>(100, 250, 150, 150);
        /// <summary>When true, extra diagnostic log messages and GPS markers are emitted to help identify issues during testing.</summary>
        public UserSetting<bool> DebugMode = new UserSetting<bool>(false, true, false, false);
        /// <summary>When true, also logs high-frequency internal events (pool ops, per-grid init steps, topology fan-out). Enabling VerboseMode implies DebugMode.</summary>
        public UserSetting<bool> VerboseMode = new UserSetting<bool>(false, true, false, false);
        /// <summary>Set of subsystems whose debug output is enabled when DebugMode is active.  Populated by <see cref="InitializeToDefaults"/> (all categories).  VerboseMode always enables all categories regardless of this value.</summary>
        public HashSet<LogCategory> ActiveDebugCategories = new HashSet<LogCategory>();

        // ── Computed debug-level helpers ─────────────────────────────────────────────
        // Use these everywhere instead of DebugMode.Current directly, so VerboseMode
        // automatically implies debug output without needing explicit OR checks at each site.

        /// <summary>True when either DebugMode or VerboseMode is active.  Use <see cref="IsDebugActiveFor"/> for category-filtered gating.</summary>
        public bool IsDebugActive => DebugMode.Current || VerboseMode.Current;
        /// <summary>True only when VerboseMode is active.  Use <see cref="IsVerboseActiveFor"/> for category-filtered gating.</summary>
        public bool IsVerboseActive => VerboseMode.Current;

        /// <summary>
        /// True when debug output is active for the given subsystem category.
        /// VerboseMode implies all categories; DebugMode checks <see cref="ActiveDebugCategories"/>.
        /// </summary>
        public bool IsDebugActiveFor(LogCategory category) =>
            (DebugMode.Current || VerboseMode.Current) &&
            (VerboseMode.Current || ActiveDebugCategories.Contains(category));

        /// <summary>True only when VerboseMode is active.  VerboseMode enables all categories regardless of <see cref="ActiveDebugCategories"/>.</summary>
        public bool IsVerboseActiveFor(LogCategory category) => VerboseMode.Current;

        // ── Mod hard-coded tick delays ───────────────────────────────────────────────
        // These values control internal timing and are not exposed to users.

        /// <summary>Ticks to wait after a block is added before classifying it (allows full initialisation).
        /// 60 ticks (1 second) gives SE enough time to fully wire up block components even under
        /// heavy world-load conditions where many grids initialise simultaneously.</summary>
        public const int BlockAddTickDelay = 10;
        /// <summary>Ticks to wait after an entity spawns before inspecting it.</summary>
        public const int EntityAddTickDelay = 10;
        /// <summary>Ticks to wait before re-running grinder logic after a grinder spawns.</summary>
        public const int GrinderTickDelay = 10;
        /// <summary>Short delay (ticks) used for non-critical deferred actions.</summary>
        public const int MinorTickDelay = 2;
        /// <summary>Ticks between re-checks of temporarily-invalid grids (3 minutes at 60 tps).</summary>
        public const int RecheckGridInterval = Common.References.TicksPerMinute * 3;
        /// <summary>Ticks to wait after the first ownership-change event before evaluating group ownership.
        /// Batches rapid-fire OnBlockOwnershipChanged callbacks that arrive during a grind pass.</summary>
        public const int OwnershipChangeDebounceDelay = 30;

        // ── Highlight visual constants ───────────────────────────────────────────────
        // These control the appearance of the block highlights shown to the grinder user.

        /// <summary>How long (in ticks) a highlight remains active before automatically turning off.</summary>
        public readonly int HighlightDuration = Common.References.TicksPerSecond * 10;
        /// <summary>Duration of the highlight pulse animation in ticks (controls pulse speed).</summary>
        public readonly int HighlightPulseDuration = 120;
        /// <summary>Line thickness used when a highlight is active (visible to the player).</summary>
        public readonly int EnabledThickness = 10;
        /// <summary>Line thickness used when disabling a highlight (-1 = remove from HUD).</summary>
        public readonly int DisabledThickness = -1;
        /// <summary>Highlight colour for control seats.</summary>
        public readonly Color ControlColor = Color.DodgerBlue;
        /// <summary>Highlight colour for medical / cryo / survival-kit blocks.</summary>
        public readonly Color MedicalColor = Color.Red;
        /// <summary>Highlight colour for weapon blocks.</summary>
        public readonly Color WeaponColor = Color.Purple;
        /// <summary>Highlight colour for warhead / trap blocks.</summary>
        public readonly Color TrapColor = Color.LightSeaGreen;

        /// <summary>
        /// Populates <see cref="ActiveDebugCategories"/> with all registered functional
        /// categories (those with Id != 0), equivalent to the "All" setting.
        /// Must be called after <c>DebugType.Initialize()</c> has run (via <c>EarlyInit</c>)
        /// so the <see cref="LogCategory"/> registry is fully populated.
        /// </summary>
        public void InitializeToDefaults()
        {
            ActiveDebugCategories.Clear();
            foreach (var cat in LogCategory.AllRegistered)
            {
                if (cat.Id != 0) ActiveDebugCategories.Add(cat);
            }
        }

        /// <summary>
        /// Serializes <see cref="ActiveDebugCategories"/> to a canonical string for
        /// storage in the XML settings file and sandbox variables.
        /// Returns "All" when all functional categories are active, "None" when empty,
        /// or a comma-separated list of active category names otherwise.
        /// </summary>
        public string SerializeActiveCategories()
        {
            if (ActiveDebugCategories.Count == 0) return "None";

            bool allActive = true;
            foreach (var cat in LogCategory.AllRegistered)
            {
                if (cat.Id != 0 && !ActiveDebugCategories.Contains(cat))
                {
                    allActive = false;
                    break;
                }
            }
            if (allActive) return "All";

            var sb = new StringBuilder();
            bool first = true;
            foreach (var cat in ActiveDebugCategories)
            {
                if (!first) sb.Append(", ");
                sb.Append(cat.Name);
                first = false;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Copies the current default values into <paramref name="userSettings"/> as
        /// serialisable strings, producing the initial XML file content on first run.
        /// </summary>
        public void CopyTo(UserSettings userSettings)
        {
            userSettings.EntityDetectionRange                      = EntityDetectionRange.ToString();
            userSettings.AllowPlayerHacking                        = AllowPlayerHacking.ToString().ToLower();
            userSettings.MirrorEasyNpcTakeovers                   = MirrorEasyNpcTakeovers.ToString().ToLower();
            userSettings.UseHighlights                             = UseHighlights.ToString().ToLower();
            userSettings.HighlightAllGridsInRange                  = HighlightAllGridsInRange.ToString().ToLower();
            userSettings.UseWeaponGroup                            = UseWeaponGroup.ToString().ToLower();
            userSettings.UseMedicalGroup                           = UseMedicalGroup.ToString().ToLower();
            userSettings.UseTrapGroup                              = UseTrapGroup.ToString().ToLower();
            userSettings.HighlightAllBlocks                        = HighlightAllBlocks.ToString().ToLower();
            userSettings.HighlightSingleNearestBlock               = HighlightSingleNearestBlock.ToString().ToLower();
            userSettings.HighlightSingleNearestBlockInActiveGroup  = HighlightSingleNearestBlockInActiveGroup.ToString().ToLower();
            userSettings.UseGrinderTierHighlighting                = UseGrinderTierHighlighting.ToString().ToLower();
            userSettings.BlocksPerGrinderTier                      = BlocksPerGrinderTier.ToString();
            userSettings.UnknownGrinderTierBlockCount              = UnknownGrinderTierBlockCount.ToString();
            userSettings.DebugMode                                 = DebugMode.ToString().ToLower();
            userSettings.VerboseMode                               = VerboseMode.ToString().ToLower();
            userSettings.ActiveDebugCategories                     = SerializeActiveCategories();
        }

        /// <summary>
        /// Returns a formatted summary of all active settings values, suitable for
        /// inclusion in a startup diagnostic log.
        /// </summary>
        public StringBuilder PrintSettings()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("\n\n{0, -2}{1} Settings", " ", "Hostile Takeover");
            sb.AppendLine("__________________________________________________\n");
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", EntityDetectionRange,                      nameof(EntityDetectionRange));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", AllowPlayerHacking,                        nameof(AllowPlayerHacking));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", MirrorEasyNpcTakeovers,                    nameof(MirrorEasyNpcTakeovers));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", UseHighlights,                             nameof(UseHighlights));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", HighlightAllGridsInRange,                  nameof(HighlightAllGridsInRange));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", UseWeaponGroup,                            nameof(UseWeaponGroup));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", UseMedicalGroup,                           nameof(UseMedicalGroup));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", UseTrapGroup,                              nameof(UseTrapGroup));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", HighlightAllBlocks,                        nameof(HighlightAllBlocks));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", HighlightSingleNearestBlock,               nameof(HighlightSingleNearestBlock));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", HighlightSingleNearestBlockInActiveGroup,  nameof(HighlightSingleNearestBlockInActiveGroup));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", UseGrinderTierHighlighting,                nameof(UseGrinderTierHighlighting));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", BlocksPerGrinderTier,                     nameof(BlocksPerGrinderTier));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", UnknownGrinderTierBlockCount,             nameof(UnknownGrinderTierBlockCount));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", DebugMode,                                 nameof(DebugMode));
            sb.AppendFormat("{0, -4}[{1}] {2}\n",   " ", VerboseMode,                               nameof(VerboseMode));
            sb.AppendFormat("{0, -4}[{1}] {2}\n\n", " ", SerializeActiveCategories(),               nameof(ActiveDebugCategories));
            return sb;
        }
    }
}
