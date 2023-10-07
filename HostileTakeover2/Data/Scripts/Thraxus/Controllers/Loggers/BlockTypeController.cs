using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class BlockTypeController : BaseLoggingClass
    {
        private readonly Dictionary<BlockType, HashSet<Block>> _importantBlocks =
            new Dictionary<BlockType, HashSet<Block>>
            {
                { BlockType.Control, new HashSet<Block>() },
                { BlockType.Medical, new HashSet<Block>() },
                { BlockType.Trap, new HashSet<Block>() },
                { BlockType.Weapon, new HashSet<Block>() }
            };

        private Mediator _mediator;
        private GridOwnershipController _ownershipController;

        public void Init(Mediator mediator, GridOwnershipController ownershipController)
        {
            _mediator = mediator;
            _ownershipController = ownershipController;
            IsClosed = false;
        }

        public void AddGrid(MyCubeGrid grid)
        {
            foreach (var block in grid.GetFatBlocks())
            {
                AddBlock(block);
            }
        }

        public void AddBlock(MyCubeBlock myCubeBlock)
        {
            _mediator.ActionQueue.Add(10, () =>
            {
                var blockType = AssignBlock(myCubeBlock);
                if (blockType == BlockType.None) return;
                IsClosed = false;
                //Block block = _mediator.BlockPool.Get();
                Block block = _mediator.GetBlock();
                block.Initialize(blockType, myCubeBlock, _ownershipController);
                block.OnClose += CloseBlock;
                _importantBlocks[blockType].Add(block);
            });
        }

        private void CloseBlock(IClose block)
        {
            _importantBlocks[((Block)block).BlockType].Remove((Block)block);
            //_mediator.BlockPool.Return((Block)block);
            _mediator.ReturnBlock((Block)block);
        }
        
        private BlockType AssignBlock(MyCubeBlock block)
        {
            WriteGeneral(nameof(AssignBlock), $"Attempting to classify new block...");
            var controller = block as IMyShipController;
            if (controller != null && controller.CanControlShip)
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Control}...");
                return BlockType.Control;
            }

            var medical = block as IMyMedicalRoom;
            if (medical != null)
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Medical}...");
                return BlockType.Medical;
            }

            var cryo = block as IMyCryoChamber;
            if (cryo != null)
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Medical}...");
                return BlockType.Medical;
            }

            var weapon = block as IMyLargeTurretBase;
            if (weapon != null)
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Weapon}...");
                return BlockType.Weapon;
            }

            var sorter = block as MyConveyorSorter;
            if (sorter != null && !sorter.BlockDefinition.Context.IsBaseGame)
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Weapon}...");
                return BlockType.Weapon;
            }

            var warhead = block as IMyWarhead;
            if (warhead != null)
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Trap}...");
                return BlockType.Trap;
            }

            if (block.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_SurvivalKit))
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Medical}...");
                return BlockType.Medical;
            }

            var upgrade = block as IMyUpgradeModule;
            if (upgrade != null && block.BlockDefinition.Id.SubtypeId == MyStringHash.GetOrCompute("BotSpawner"))
            {
                WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Weapon}...");
                return BlockType.Weapon;
            }
            WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.None}...");
            return BlockType.None;
        }

        public override void Reset()
        {
            base.Reset();
            foreach (var kvp in _importantBlocks)
            {
                kvp.Value.Clear();
            }
        }

        public Dictionary<BlockType, HashSet<Block>> GetImportantBlockDictionary()
        {
            return _importantBlocks;
        }
    }
}