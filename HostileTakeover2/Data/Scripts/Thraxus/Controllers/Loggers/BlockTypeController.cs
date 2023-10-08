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
        private readonly Dictionary<MyCubeBlock, Block> _importantBlocks =
            new Dictionary<MyCubeBlock, Block>();

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
            foreach (var fatBlock in grid.GetFatBlocks())
            {
                AddBlock(fatBlock);
            }
        }

        public void RemoveOldBlocks(MyCubeGrid grid)
        {
            foreach (var fatBlock in grid.GetFatBlocks())
            {
                ResetBlock(fatBlock);
            }
        }
        
        public void AddBlock(MyCubeBlock myCubeBlock)
        {
            _mediator.ActionQueue.Add(10, () =>
            {
                var blockType = AssignBlock(myCubeBlock);
                if (blockType == BlockType.None) return;
                if (_importantBlocks.ContainsKey(myCubeBlock)) return;
                IsClosed = false;
                Block block = _mediator.GetBlock(myCubeBlock.EntityId);
                block.Initialize(blockType, myCubeBlock, _ownershipController);
                RegisterBlockEvents(block);
                AddToDictionary(myCubeBlock, block);
            });
        }

        private void AddToDictionary(MyCubeBlock myCubeBlock, Block block)
        {
            if (_importantBlocks.ContainsKey(myCubeBlock)) return;
            _importantBlocks.Add(myCubeBlock, block);
        }

        private void RemoveFromDictionary(MyCubeBlock myCubeBlock)
        {
            if (!_importantBlocks.ContainsKey(myCubeBlock)) return;
            _importantBlocks.Remove(myCubeBlock);
        }

        private void RegisterBlockEvents(Block block)
        {
            block.OnReset += OnResetBlock;
            block.OnClose += BlockOnClose;
        }

        private void BlockOnClose(IClose block)
        {
            if (!_importantBlocks.ContainsKey(((Block)block).MyCubeBlock)) return;
            OnResetBlock(_importantBlocks[((Block)block).MyCubeBlock]);
        }

        private void DeRegisterBlockEvents(Block block)
        {
            block.OnReset -= OnResetBlock;
            block.OnClose -= BlockOnClose;
        }

        private void OnResetBlock(IReset block)
        {
            DeRegisterBlockEvents((Block)block);
            RemoveFromDictionary(((Block)block).MyCubeBlock);
            _mediator.ReturnBlock((Block)block, ((Block)block).EntityId);
        }

        private void ResetBlock(MyCubeBlock myCubeBlock)
        {
            if (!_importantBlocks.ContainsKey(myCubeBlock)) return;
            OnResetBlock(_importantBlocks[myCubeBlock]);
        }
        
        private BlockType AssignBlock(MyCubeBlock block)
        {
            //WriteGeneral(nameof(AssignBlock), $"Attempting to classify new block...");
            var controller = block as IMyShipController;
            if (controller != null && controller.CanControlShip)
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Control}...");
                return BlockType.Control;
            }

            var medical = block as IMyMedicalRoom;
            if (medical != null)
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Medical}...");
                return BlockType.Medical;
            }

            var cryo = block as IMyCryoChamber;
            if (cryo != null)
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Medical}...");
                return BlockType.Medical;
            }

            var weapon = block as IMyLargeTurretBase;
            if (weapon != null)
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Weapon}...");
                return BlockType.Weapon;
            }

            var sorter = block as MyConveyorSorter;
            if (sorter != null && !sorter.BlockDefinition.Context.IsBaseGame)
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Weapon}...");
                return BlockType.Weapon;
            }

            var warhead = block as IMyWarhead;
            if (warhead != null)
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Trap}...");
                return BlockType.Trap;
            }

            if (block.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_SurvivalKit))
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Medical}...");
                return BlockType.Medical;
            }

            var upgrade = block as IMyUpgradeModule;
            if (upgrade != null && block.BlockDefinition.Id.SubtypeId == MyStringHash.GetOrCompute("BotSpawner"))
            {
                //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.Weapon}...");
                return BlockType.Weapon;
            }
            //WriteGeneral(nameof(AssignBlock), $"Block classified as {BlockType.None}...");
            return BlockType.None;
        }

        private void SoftReset()
        {
            foreach (var kvp in _importantBlocks)
            {
                Block block = kvp.Value;
                DeRegisterBlockEvents(block);
                _mediator.ReturnBlock(block, block.EntityId);
            }
            _importantBlocks.Clear();
        }

        public override void Reset()
        {
            base.Reset();
            SoftReset();
        }

        public Dictionary<MyCubeBlock, Block> GetImportantBlockDictionary()
        {
            return _importantBlocks;
        }
    }
}