using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Utilities.Tools.Research;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools
{
    /// <summary>
    /// Controls which research outputs are active. Toggle the flags before calling
    /// <see cref="Initialize"/> to select what gets dumped. Each active dumper spins
    /// up its own log, writes its output, and tears itself down.
    ///
    /// Owns its own <see cref="ActionQueue"/> driven by the caller via <see cref="Tick"/>,
    /// keeping this utility fully decoupled from the host mod's infrastructure.
    /// </summary>
    internal static class ResearchManager
    {
        private static readonly ActionQueue ActionQueue = new ActionQueue();

        public static bool RunBlockDefinitionDump = true;
        public static bool RunFilteredBlockDefinitionDump = true;
        public static bool RunControlBlockDefinitionDump = true;
        public static bool RunMedicalBlockDefinitionDump = true;
        public static bool RunWeaponBlockDefinitionDump = true;
        public static bool RunTrapBlockDefinitionDump = true;
        public static bool RunModdedBlockDefinitionDump = true;
        public static bool RunBlockResearch = false;

        public static void Initialize()
        {
            if (RunBlockDefinitionDump) BlockDefinitionDumper.Dump();
            if (RunFilteredBlockDefinitionDump) FilteredBlockDefinitionDumper.Dump();
            if (RunControlBlockDefinitionDump) ControlBlockDefinitionDumper.Dump();
            if (RunMedicalBlockDefinitionDump) MedicalBlockDefinitionDumper.Dump();
            if (RunWeaponBlockDefinitionDump) WeaponBlockDefinitionDumper.Dump();
            if (RunTrapBlockDefinitionDump) TrapBlockDefinitionDumper.Dump();
            if (RunModdedBlockDefinitionDump) ModdedBlockDefinitionDumper.Dump();
            if (RunBlockResearch)
            {
                RuntimeBlockInterfaceDumper.Activate();
                int count = BlockResearchSpawner.QueueSpawns(ActionQueue);
                ActionQueue.Add(count * 5 + 300, RuntimeBlockInterfaceDumper.Flush);
            }
        }

        /// <summary>Called once per game tick from the host session component to drive deferred research actions.</summary>
        public static void Tick()
        {
            ActionQueue.Execute();
        }

        /// <summary>Clears all pending deferred actions. Call from the host session component's unload path.</summary>
        public static void Unload()
        {
            ActionQueue.Reset();
        }
    }
}
