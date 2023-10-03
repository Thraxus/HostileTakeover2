using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Settings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class BlockController : BaseLoggingClass
    {
        private readonly Dictionary<BlockType, HashSet<Block>> _importantBlocks =
            new Dictionary<BlockType, HashSet<Block>>
            {
                { BlockType.Control, new HashSet<Block>() },
                { BlockType.Medical, new HashSet<Block>() },
                { BlockType.Trap, new HashSet<Block>() },
                { BlockType.Weapon, new HashSet<Block>() }
            };

        private Utilities _utilities;
        private OwnershipController _ownershipController;

        public void Init(Utilities utilities, OwnershipController ownershipController)
        {
            _utilities = utilities;
            _ownershipController = ownershipController;
        }

        public void AddBlock(MyCubeBlock myCubeBlock)
        {
            _utilities.ActionQueue.Add(10, () =>
            {
                var blockType = AssignBlock(myCubeBlock);
                if (blockType == BlockType.None) return;
                Block block = _utilities.BlockPool.Get();
                block.Initialize(blockType, myCubeBlock, _ownershipController);
                block.OnClose += CloseBlock;
                _importantBlocks[blockType].Add(block);
            });
        }

        private void CloseBlock(IClose block)
        {
            _importantBlocks[((Block)block).BlockType].Remove((Block)block);
            _utilities.BlockPool.Return((Block)block);
        }
        
        private BlockType AssignBlock(MyCubeBlock block)
        {
            var controller = block as IMyShipController;
            if (controller != null && controller.CanControlShip)
                return BlockType.Control;

            var medical = block as IMyMedicalRoom;
            if (medical != null)
                return BlockType.Medical;

            var cryo = block as IMyCryoChamber;
            if (cryo != null)
                return BlockType.Medical;

            var weapon = block as IMyLargeTurretBase;
            if (weapon != null)
                return BlockType.Weapon;

            var sorter = block as MyConveyorSorter;
            if (sorter != null && !sorter.BlockDefinition.Context.IsBaseGame)
                return BlockType.Weapon;

            var warhead = block as IMyWarhead;
            if (warhead != null)
                return BlockType.Trap;

            if (block.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_SurvivalKit))
                return BlockType.Medical;

            var upgrade = block as IMyUpgradeModule;
            if (upgrade != null && block.BlockDefinition.Id.SubtypeId == MyStringHash.GetOrCompute("BotSpawner"))
                return BlockType.Weapon;

            return BlockType.None;
        }

        public void HighlightNextSet()
        {
            if (_importantBlocks[BlockType.Control].Count > 0)
            {
                _utilities.HighlightController.HighlightBlocks(_importantBlocks[BlockType.Control]);
                return;
            }
            if (_importantBlocks[BlockType.Medical].Count > 0 && DefaultSettings.UseMedicalGroup.Current)
            {
                _utilities.HighlightController.HighlightBlocks(_importantBlocks[BlockType.Medical]);
                return;
            }
            if (_importantBlocks[BlockType.Weapon].Count > 0 && DefaultSettings.UseWeaponGroup.Current)
            {
                _utilities.HighlightController.HighlightBlocks(_importantBlocks[BlockType.Weapon]);
                return;
            }
            if (_importantBlocks[BlockType.Trap].Count > 0 && DefaultSettings.UseTrapGroup.Current)
            {
                _utilities.HighlightController.HighlightBlocks(_importantBlocks[BlockType.Trap]);
                return;
            }
        }
    }
}