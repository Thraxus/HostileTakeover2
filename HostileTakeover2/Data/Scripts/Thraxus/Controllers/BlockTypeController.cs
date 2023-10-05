using System.Collections;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Controllers
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
            IsClosed = true;
        }

        public void AddBlock(MyCubeBlock myCubeBlock)
        {
            _mediator.ActionQueue.Add(10, () =>
            {
                var blockType = AssignBlock(myCubeBlock);
                if (blockType == BlockType.None) return;
                IsClosed = false;
                Block block = _mediator.BlockPool.Get();
                block.Initialize(blockType, myCubeBlock, _ownershipController);
                block.OnClose += CloseBlock;
                _importantBlocks[blockType].Add(block);
            });
        }

        private void CloseBlock(IClose block)
        {
            _importantBlocks[((Block)block).BlockType].Remove((Block)block);
            _mediator.BlockPool.Return((Block)block);
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