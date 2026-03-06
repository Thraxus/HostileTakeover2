using HostileTakeover2.Thraxus.Infrastructure;

namespace HostileTakeover2.Thraxus.Utility.Research
{
    /// <summary>
    /// Controls which research outputs are active. Toggle the flags before calling
    /// <see cref="Initialize"/> to select what gets dumped. Each active dumper spins
    /// up its own log, writes its output, and tears itself down.
    /// </summary>
    internal static class ResearchManager
    {
        public static bool RunBlockDefinitionDump = true;
        public static bool RunFilteredBlockDefinitionDump = true;
        public static bool RunBlockResearch = true;

        public static void Initialize(Mediator mediator)
        {
            if (RunBlockDefinitionDump) BlockDefinitionDumper.Dump();
            if (RunFilteredBlockDefinitionDump) FilteredBlockDefinitionDumper.Dump();
            if (RunBlockResearch)
            {
                RuntimeBlockInterfaceDumper.Activate();
                int count = BlockResearchSpawner.QueueSpawns(mediator.ActionQueue);
                mediator.ActionQueue.Add(count * 5 + 300, RuntimeBlockInterfaceDumper.Flush);
            }
        }
    }
}
