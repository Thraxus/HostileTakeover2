using System.Collections.Generic;
using System.Text;

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

        // Base counts captured after Populate() but before overrides are applied.
        // Used to show current/base in the startup log so overrides are immediately visible.
        private int _baseControlCount;
        private int _baseMedicalCount;
        private int _baseWeaponCount;
        private int _baseTrapCount;

        public void Clear()
        {
            ControlBlocks.Clear();
            MedicalBlocks.Clear();
            WeaponBlocks.Clear();
            TrapBlocks.Clear();
        }

        /// <summary>Snapshot counts before overrides are applied. Call after Populate, before Read.</summary>
        public void LockBaseCounts()
        {
            _baseControlCount = ControlBlocks.Count;
            _baseMedicalCount = MedicalBlocks.Count;
            _baseWeaponCount  = WeaponBlocks.Count;
            _baseTrapCount    = TrapBlocks.Count;
        }

        public string PrintCountsSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n\n  Block Classification Counts__________________________________________________\n");
            AppendCountLine(sb, "Control", ControlBlocks.Count, _baseControlCount);
            AppendCountLine(sb, "Medical", MedicalBlocks.Count, _baseMedicalCount);
            AppendCountLine(sb, "Weapon",  WeaponBlocks.Count,  _baseWeaponCount);
            AppendCountLine(sb, "Trap",    TrapBlocks.Count,    _baseTrapCount);
            return sb.ToString();
        }

        private static void AppendCountLine(StringBuilder sb, string category, int current, int @base)
        {
            int delta = current - @base;
            string note = delta == 0 ? "(none overridden)"
                        : delta  < 0 ? $"({-delta} excluded)"
                        :              $"({delta} added)";
            sb.AppendLine($"    [{category,-8}]  {current}/{@base} {note}");
        }
    }
}
