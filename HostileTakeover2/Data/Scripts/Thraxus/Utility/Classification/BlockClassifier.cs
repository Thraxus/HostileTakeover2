using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Definitions;

namespace HostileTakeover2.Thraxus.Utility.Classification
{
    /// <summary>
    /// Scrapes MyDefinitionManager at mod init and populates a <see cref="BlockClassificationData"/>
    /// with the TypeId/SubtypeId keys for every block that belongs to each category.
    /// This runs once per session load; BlockController.AssignBlock then does a simple
    /// HashSet lookup instead of a chain of runtime interface casts.
    ///
    /// MyCryoChamberDefinition inherits from MyCockpitDefinition, so it must be checked
    /// before the cockpit/control pass to avoid being misclassified as Control.
    /// Only cryo blocks with ResourceSinkGroup == "LifeSupport" are included as Medical;
    /// decorative beds and other non-functional cryo variants are excluded.
    /// </summary>
    internal static class BlockClassifier
    {
        public static void Populate(BlockClassificationData data, DefaultSettings settings)
        {
            data.Clear();

            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var cubeDef = def as MyCubeBlockDefinition;
                if (cubeDef == null) continue;

                string key = cubeDef.Id.ToString();

                // Cryo must be checked before MyCockpitDefinition because
                // MyCryoChamberDefinition : MyCockpitDefinition.
                var cryo = def as MyCryoChamberDefinition;
                if (cryo != null)
                {
                    if (cryo.ResourceSinkGroup == "LifeSupport")
                        data.MedicalBlocks.Add(key);
                    continue;
                }

                // Control
                if (def is MyCockpitDefinition || def is MyRemoteControlDefinition || def is MyDefensiveCombatBlockDefinition)
                {
                    data.ControlBlocks.Add(key);
                    continue;
                }

                // Medical (non-cryo)
                if (def is MyMedicalRoomDefinition || def is MySurvivalKitDefinition)
                {
                    data.MedicalBlocks.Add(key);
                    continue;
                }

                // Weapon — vanilla autonomous turrets and offensive AI blocks
                if (def is MyLargeTurretBaseDefinition || def is MyOffensiveCombatBlockDefinition)
                {
                    data.WeaponBlocks.Add(key);
                    continue;
                }

                // Weapon — modded sorters (WeaponCore or AiEnabled)
                if (settings.IsWeaponCoreActive || settings.IsAiEnabledActive)
                {
                    var sorter = def as MyConveyorSorterDefinition;
                    if (sorter != null && sorter.Context != null && !sorter.Context.IsBaseGame)
                    {
                        data.WeaponBlocks.Add(key);
                        continue;
                    }
                }

                // Weapon — modded upgrade modules (AiEnabled only)
                if (settings.IsAiEnabledActive)
                {
                    var upgrade = def as MyUpgradeModuleDefinition;
                    if (upgrade != null && upgrade.Context != null && !upgrade.Context.IsBaseGame)
                    {
                        data.WeaponBlocks.Add(key);
                        continue;
                    }
                }

                // Trap
                if (def is MyWarheadDefinition)
                {
                    data.TrapBlocks.Add(key);
                }
            }
        }
    }
}
