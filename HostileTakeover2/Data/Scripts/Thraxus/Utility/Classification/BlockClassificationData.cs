using System.Collections.Generic;

namespace HostileTakeover2.Thraxus.Utility.Classification
{
    /// <summary>
    /// Holds the four per-category HashSets produced by <see cref="BlockClassifier"/>.
    /// Keys are MyDefinitionId.ToString() strings ("TypeId/SubtypeId").
    /// Consumed by BlockController.AssignBlock for O(1) lookup instead of runtime casts.
    /// </summary>
    internal class BlockClassificationData
    {
        public readonly HashSet<string> ControlBlocks = new HashSet<string>();
        public readonly HashSet<string> MedicalBlocks = new HashSet<string>();
        public readonly HashSet<string> WeaponBlocks  = new HashSet<string>();
        public readonly HashSet<string> TrapBlocks    = new HashSet<string>();

        public void Clear()
        {
            ControlBlocks.Clear();
            MedicalBlocks.Clear();
            WeaponBlocks.Clear();
            TrapBlocks.Clear();
        }
    }
}
