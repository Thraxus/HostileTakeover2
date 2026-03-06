using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    internal static class RuntimeBlockInterfaceDumper
    {
        private class BlockInterfaceRecord
        {
            public string TypeName;
            public string TypeId;
            public string SubtypeId;
            public string CubeSize;
            public string IsIMyFunctionalBlock;
            public string IsIMyShipController;
            public string CanControlShip;
            public string IsIMyRemoteControl;
            public string IsIMyMedicalRoom;
            public string IsIMyCryoChamber;
            public string IsIMyLargeTurretBase;
            public string IsIMyWarhead;
            public string IsIMyUpgradeModule;
            public string IsIMyConveyorSorter;
            public string IsIMyTurretControlBlock;
            public string IsIMyDefensiveCombatBlock;
            public string IsIMyOffensiveCombatBlock;
            public string IsIMyAiBlockComponent;
        }

        private static readonly List<BlockInterfaceRecord> Records = new List<BlockInterfaceRecord>();
        private static readonly HashSet<string> _seen = new HashSet<string>();

        public static void Activate()
        {
            RuntimeBlockLogger.OnBlockEncountered += OnBlockEncountered;
        }

        private static void OnBlockEncountered(MyCubeBlock block)
        {
            try
            {
                if (!BlockResearchSpawner.TargetTypes.Contains(block.BlockDefinition.GetType().Name)) return;
                string key = $"{block.BlockDefinition.Id.TypeId}|{block.BlockDefinition.Id.SubtypeId.String}|{block.BlockDefinition.CubeSize}";
                if (!_seen.Add(key)) return;
                var shipController = block as IMyShipController;
                var cryo = block as IMyCryoChamber;
                Records.Add(new BlockInterfaceRecord
                {
                    TypeName = block.GetType().Name,
                    TypeId = block.BlockDefinition.Id.TypeId.ToString(),
                    SubtypeId = block.BlockDefinition.Id.SubtypeId.String,
                    CubeSize = block.BlockDefinition.CubeSize.ToString(),
                    IsIMyFunctionalBlock  = Y(block as IMyFunctionalBlock),
                    IsIMyShipController   = Y(shipController),
                    CanControlShip        = (shipController != null && shipController.CanControlShip) || (cryo != null && cryo.CanControlShip) ? "Y" : string.Empty,
                    IsIMyRemoteControl    = Y(block as IMyRemoteControl),
                    IsIMyMedicalRoom      = Y(block as IMyMedicalRoom),
                    IsIMyCryoChamber      = Y(cryo),
                    IsIMyLargeTurretBase  = Y(block as IMyLargeTurretBase),
                    IsIMyWarhead          = Y(block as IMyWarhead),
                    IsIMyUpgradeModule    = Y(block as IMyUpgradeModule),
                    IsIMyConveyorSorter   = Y(block as IMyConveyorSorter),
                    IsIMyTurretControlBlock = Y(block as IMyTurretControlBlock),
                    IsIMyDefensiveCombatBlock = Y(block as IMyDefensiveCombatBlock),
                    IsIMyOffensiveCombatBlock = Y(block as IMyOffensiveCombatBlock),
                    IsIMyAiBlockComponent = Y(block as IMyAiBlockComponent),
                });
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"RuntimeBlockInterfaceDumper.OnBlockEncountered() exception: {e}");
            }
        }

        private static string Y(object o) => o != null ? "Y" : string.Empty;

        public static void Flush()
        {
            RuntimeBlockLogger.OnBlockEncountered -= OnBlockEncountered;

            Records.Sort((a, b) =>
            {
                int c = string.Compare(a.TypeName, b.TypeName, StringComparison.Ordinal);
                if (c != 0) return c;
                c = string.Compare(a.TypeId, b.TypeId, StringComparison.Ordinal);
                if (c != 0) return c;
                c = string.Compare(a.SubtypeId, b.SubtypeId, StringComparison.Ordinal);
                if (c != 0) return c;
                return string.Compare(a.CubeSize, b.CubeSize, StringComparison.Ordinal);
            });

            var data = new DataLog("RuntimeBlockInterfaces");
            try
            {
                string lastTypeId = null;
                foreach (var r in Records)
                {
                    if (r.TypeId != lastTypeId)
                    {
                        if (lastTypeId != null) data.Write(string.Empty);
                        data.Write($"## {r.TypeName}  ({r.TypeId})");
                        data.Write(string.Empty);
                        data.WriteHeader(
                            "SubtypeId", "CubeSize",
                            "IMyFunctionalBlock", "IMyShipController", "CanControlShip", "IMyRemoteControl",
                            "IMyMedicalRoom", "IMyCryoChamber", "IMyLargeTurretBase", "IMyWarhead",
                            "IMyUpgradeModule", "IMyConveyorSorter", "IMyTurretControlBlock",
                            "IMyDefensiveCombatBlock", "IMyOffensiveCombatBlock", "IMyAiBlockComponent"
                        );
                        lastTypeId = r.TypeId;
                    }
                    data.WriteRow(
                        r.SubtypeId, r.CubeSize,
                        r.IsIMyFunctionalBlock, r.IsIMyShipController, r.CanControlShip, r.IsIMyRemoteControl,
                        r.IsIMyMedicalRoom, r.IsIMyCryoChamber, r.IsIMyLargeTurretBase, r.IsIMyWarhead,
                        r.IsIMyUpgradeModule, r.IsIMyConveyorSorter, r.IsIMyTurretControlBlock,
                        r.IsIMyDefensiveCombatBlock, r.IsIMyOffensiveCombatBlock, r.IsIMyAiBlockComponent
                    );
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"RuntimeBlockInterfaceDumper.Flush() write exception: {e}");
            }
            finally
            {
                data.Close();
                Records.Clear();
                _seen.Clear();
            }

            BlockResearchSpawner.Cleanup();
            MyAPIGateway.Utilities.ShowMessage("Research", "Interface dump complete \u2192 RuntimeBlockInterfaces.md");
        }
    }
}
