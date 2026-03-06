using System;
using Sandbox.Game.Entities;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    internal static class RuntimeBlockLogger
    {
        /// <summary>
        /// Invoked when a block is encountered during classification.
        /// Wire this from the appropriate call site to capture runtime block data.
        /// The handler receives the block as-is; extract TypeId, SubtypeId, Context, etc. as needed.
        /// </summary>
        public static Action<MyCubeBlock> OnBlockEncountered;
    }
}
