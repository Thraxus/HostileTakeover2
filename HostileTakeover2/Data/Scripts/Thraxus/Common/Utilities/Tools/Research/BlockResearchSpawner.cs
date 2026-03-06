using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.Generics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    internal static class BlockResearchSpawner
    {
        internal static readonly HashSet<string> TargetTypes = new HashSet<string>
        {
            "MyCockpitDefinition",
            "MyConveyorSorterDefinition",
            "MyCryoChamberDefinition",
            "MyDefensiveCombatBlockDefinition",
            "MyLargeTurretBaseDefinition",
            "MyMedicalRoomDefinition",
            "MyOffensiveCombatBlockDefinition",
            "MyRemoteControlDefinition",
            "MySurvivalKitDefinition",
            "MyTurretControlBlockDefinition",
            "MyWarheadDefinition",
            "MyWeaponBlockDefinition",
        };

        private static readonly List<long> SpawnedEntityIds = new List<long>();

        public static int QueueSpawns(ActionQueue actionQueue)
        {
            var defs = new List<MyCubeBlockDefinition>();
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var cubeDef = def as MyCubeBlockDefinition;
                if (cubeDef == null || !cubeDef.Public) continue;
                if (!TargetTypes.Contains(cubeDef.GetType().Name)) continue;
                defs.Add(cubeDef);
            }

            MyAPIGateway.Utilities.ShowMessage("Research", $"Spawning {defs.Count} research blocks...");

            for (int i = 0; i < defs.Count; i++)
            {
                var capturedDef = defs[i];
                var capturedIndex = i;
                actionQueue.Add(i * 5, () => SpawnBlock(capturedDef, capturedIndex));
            }

            return defs.Count;
        }

        private static void SpawnBlock(MyCubeBlockDefinition cubeDef, int index)
        {
            var blockOb = MyObjectBuilderSerializer.CreateNewObject(cubeDef.Id.TypeId) as MyObjectBuilder_CubeBlock;
            if (blockOb == null)
            {
                MyLog.Default.WriteLineAndConsole($"BlockResearchSpawner.SpawnBlock() [{cubeDef.Id}] null OB, skipping.");
                return;
            }
            blockOb.SubtypeName = cubeDef.Id.SubtypeId.String;
            blockOb.Min = Vector3I.Zero;

            var gridOb = new MyObjectBuilder_CubeGrid
            {
                GridSizeEnum = cubeDef.CubeSize,
                IsStatic = false,
                Editable = true,
                DestructibleBlocks = true,
                PersistentFlags = MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled,
                PositionAndOrientation = new MyPositionAndOrientation(
                    new Vector3D(index * 10.0, 0, 0),
                    Vector3.Forward,
                    Vector3.Up
                ),
                CubeBlocks = new List<MyObjectBuilder_CubeBlock> { blockOb }
            };

            var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridOb);
            if (entity != null)
            {
                SpawnedEntityIds.Add(entity.EntityId);
                var myCubeGrid = entity as MyCubeGrid;
                if (myCubeGrid != null)
                    foreach (var block in myCubeGrid.GetFatBlocks<MyCubeBlock>())
                        RuntimeBlockLogger.OnBlockEncountered?.Invoke(block);
            }
        }

        public static void Cleanup()
        {
            foreach (var id in SpawnedEntityIds)
            {
                IMyEntity entity;
                if (MyAPIGateway.Entities.TryGetEntityById(id, out entity) && entity != null)
                    entity.Close();
            }
            SpawnedEntityIds.Clear();
        }
    }
}
