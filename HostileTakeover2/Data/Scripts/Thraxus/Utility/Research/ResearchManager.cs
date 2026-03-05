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

        public static void Initialize()
        {
            if (RunBlockDefinitionDump) BlockDefinitionDumper.Dump();
        }
    }
}
