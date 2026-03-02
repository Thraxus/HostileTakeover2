using System.Collections.Generic;
using System.Linq;
using System.Text;
using HostileTakeover2.Thraxus.Common.Factions.DataTypes.Enums;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Common.Factions.Models
{
    /// <summary>
    /// Static registry that classifies every faction present in the current session into
    /// named dictionaries at startup.  Call <see cref="Initialize"/> once (it guards
    /// against repeat calls) and then read from the static dictionaries as needed.
    /// </summary>
    public static class FactionDictionaries
    {
        /// <summary>
        /// Normal rep controlled player factions
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> PlayerFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// Players who have decided to opt out of the rep system (always hostile to NPCs)
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> PlayerPirateFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// NPC factions who hate everyone
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> PirateFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// NPC factions who hate people who hate other people
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> EnforcementFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// NPC factions who like to be nice to everyone
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> LawfulFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// All EEM NPC factions; doesn't discriminate if they are an asshole or angel
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> AllEemNpcFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// All NPC factions that aren't controlled by EEM
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> AllNonEemNpcFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// All Vanilla Trade factions
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> VanillaTradeFactions = new Dictionary<long, IMyFaction>();

        /// <summary>
        /// All NPC Factions that aren't considered a trader (cheaty stations are cheaty)
        /// </summary>
        public static readonly Dictionary<long, IMyFaction> NonTraderNpcFactions = new Dictionary<long, IMyFaction>();

        private static bool _setupComplete;

        /// <summary>
        /// Iterates all factions in the session and classifies each one.
        /// Classification logic:
        /// <list type="number">
        ///   <item>
        ///     <c>def == null</c>: no .sbc definition exists for this faction tag.  This
        ///     means it is either a player faction, a vanilla trade station faction, or a
        ///     third-party mod faction built entirely in code.
        ///     <list type="bullet">
        ///       <item><c>TryGetSteamId(FounderId) &gt; 0</c>: a real steam account owns
        ///         this faction — it is a player faction.  If its description starts with
        ///         a known pirate prefix it is classified as a <c>PlayerPirateFaction</c>;
        ///         otherwise it is a regular <c>PlayerFaction</c>.</item>
        ///       <item>Otherwise (no steam account for the founder): classified as a
        ///         <c>VanillaTradeFaction</c> — vanilla NPC trade stations have no .sbc
        ///         definition and their founder ID maps to no steam account.</item>
        ///     </list>
        ///   </item>
        ///   <item>
        ///     <c>def != null</c>: EEM or another mod defined this faction in an .sbc file.
        ///     The faction tag is matched against the four EEM tag lists:
        ///     Neutral → <c>LawfulFactions</c>,
        ///     Enforcement → <c>EnforcementFactions</c>,
        ///     Hostile → <c>PirateFactions</c>,
        ///     Player → <c>PlayerFactions</c>.
        ///   </item>
        ///   <item>
        ///     Fallback: not matched by any EEM tag → classified as a non-EEM NPC faction
        ///     (likely a third-party NPC mod).
        ///   </item>
        /// </list>
        /// </summary>
        public static void Initialize()
        {
            if (_setupComplete) return;
            _setupComplete = true;

            foreach (var faction in MyAPIGateway.Session.Factions.Factions)
            {
                // A null definition means the faction has no .sbc file backing it.
                // This covers: player factions, vanilla trade stations, and code-only mod factions.
                MyFactionDefinition def = MyDefinitionManager.Static.TryGetFactionDefinition(faction.Value.Tag);
                if (def == null)
                {   // Player faction, Vanilla Trader, or some other mods faction that creates everything in code and nothing in the .sbc
                    // TryGetSteamId > 0 means the founder has a real steam account, so this is a player faction.
                    if (MyAPIGateway.Players.TryGetSteamId(faction.Value.FounderId) > 0)
                    {   // Player faction of some sort
                        // Description prefix check identifies player pirates who have opted out of the rep system.
                        if (Settings.PlayerFactionExclusionList.Any(x => faction.Value.Description.StartsWith(x)))
                        { // Player pirate
                            PlayerPirateFactions.Add(faction.Key, faction.Value);
                            continue;
                        }
                        // Regular player faction
                        PlayerFactions.Add(faction.Key, faction.Value);
                        continue;
                    }
                    // No steam account for the founder and no .sbc definition → vanilla trade faction.
                    VanillaTradeFactions.Add(faction.Key, faction.Value);
                    continue;
                }

                // EEM Neutral tag → lawful / friendly NPC faction.
                if (Settings.FactionTags[FactionTypes.Neutral].Contains(def.Tag))
                {
                    LawfulFactions.Add(faction.Key, faction.Value);
                    NonTraderNpcFactions.Add(faction.Key, faction.Value);
                    AllEemNpcFactions.Add(faction.Key, faction.Value);
                    continue;
                }

                // EEM Enforcement tag → NPC faction that polices hostile players.
                if (Settings.FactionTags[FactionTypes.Enforcement].Contains(def.Tag))
                {
                    EnforcementFactions.Add(faction.Key, faction.Value);
                    NonTraderNpcFactions.Add(faction.Key, faction.Value);
                    AllEemNpcFactions.Add(faction.Key, faction.Value);
                    continue;
                }

                // EEM Hostile tag → pirate / aggressive NPC faction.
                if (Settings.FactionTags[FactionTypes.Hostile].Contains(def.Tag))
                {
                    PirateFactions.Add(faction.Key, faction.Value);
                    NonTraderNpcFactions.Add(faction.Key, faction.Value);
                    AllEemNpcFactions.Add(faction.Key, faction.Value);
                    continue;
                }

                // EEM Player tag → player-aligned faction defined in an .sbc (rare edge case).
                if (Settings.FactionTags[FactionTypes.Player].Contains(def.Tag))
                {
                    PlayerFactions.Add(faction.Key, faction.Value);
                    continue;
                }

                // I'm guessing this is a NPC faction and it's not mine.
                // Fallback: has an .sbc definition but is not one of the four EEM tag categories.
                // Treat as a non-EEM NPC faction (e.g. from another NPC mod).
                AllNonEemNpcFactions.Add(faction.Key, faction.Value);
                NonTraderNpcFactions.Add(faction.Key, faction.Value);
            }
        }

        /// <summary>
        /// Builds a human-readable report of all classified factions, grouped by category.
        /// Intended for startup diagnostic logging.
        /// </summary>
        public static string Report()
        {
            StringBuilder report = new StringBuilder();
            const string x = "    ";
            report.AppendLine();
            report.AppendLine("Factions Report - Begin");
            report.AppendLine("═══════════════════════════════════════════");


            report.AppendLine("Lawful Factions");
            foreach (var faction in LawfulFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine();


            report.AppendLine("Enforcement Factions");
            foreach (var faction in EnforcementFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine();

            report.AppendLine("Pirate Factions");
            foreach (var faction in PirateFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine();

            report.AppendLine("Vanilla Trader Factions");
            if (VanillaTradeFactions.Count == 0) report.AppendLine($"{x}None");
            foreach (var faction in VanillaTradeFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine();

            report.AppendLine("Non-EEM NPC Factions");
            if (AllNonEemNpcFactions.Count == 0) report.AppendLine($"{x}None");
            foreach (var faction in AllNonEemNpcFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine();

            report.AppendLine("All NPC Factions");
            if (AllNonEemNpcFactions.Count == 0) report.AppendLine($"{x}None");
            foreach (var faction in NonTraderNpcFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine();

            report.AppendLine("Player Factions");
            if (PlayerFactions.Count == 0) report.AppendLine($"{x}None");
            foreach (var faction in PlayerFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine();

            report.AppendLine("Player Pirate Factions");
            if (PlayerPirateFactions.Count == 0) report.AppendLine($"{x}None");
            foreach (var faction in PlayerPirateFactions)
            {
                report.AppendLine($"{x}{faction.Value.Tag}");
            }

            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine("Factions Report - End");

            return report.ToString();
        }
    }
}
