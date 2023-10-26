using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models.Loggers;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class BlockTypeController : BaseLoggingClass
    {
        //private event Action RequestGridGroupOwnershipEvaluation;
        //private void TriggerRequestGridGroupOwnershipEvaluation()
        //{
        //    RequestGridGroupOwnershipEvaluation?.Invoke();
        //}

        public readonly Dictionary<long, Block> ImportantBlocks = new Dictionary<long, Block>();
        public Dictionary<long, Block> GetImportantBlockDictionary() => ImportantBlocks;

        private GridController _gridController;
        
        
        public void Init(GridController gridController)
        {
            _gridController = gridController;
        }

        public void AddGrid(MyCubeGrid grid)
        {
            WriteGeneral(nameof(AddGrid), $"Adding grid! [{grid.EntityId.ToEntityIdFormat()}]");
            foreach (var fatBlock in grid.GetFatBlocks())
            {
                AddBlock(fatBlock);
            }

            //if (ImportantBlocks.Count == 0)
            //    TriggerRequestGridGroupOwnershipEvaluation();
        }

        public void RemoveGrid(MyCubeGrid grid)
        {
            foreach (var fatBlock in grid.GetFatBlocks())
            {
                RemoveBlock(fatBlock);
            }

            //if (ImportantBlocks.Count == 0)
            //    TriggerRequestGridGroupOwnershipEvaluation();
        }
        
        public void AddBlock(MyCubeBlock myCubeBlock)
        {
            _gridController.Mediator.ActionQueue.Add(10, () =>
            {
                var blockType = AssignBlock(myCubeBlock);
                if (blockType == BlockType.None) return;
                if (ImportantBlocks.ContainsKey(myCubeBlock.EntityId)) return;
                Block block = _gridController.Mediator.GetBlock(myCubeBlock.EntityId);
                block.Init(blockType, myCubeBlock, _gridController.GridOwnership);
                RegisterBlockEvents(block);
                AddToDictionary(block);
            });
        }

        private void RemoveBlock(MyCubeBlock cubeBlock)
        {
            WriteGeneral(nameof(RemoveBlock), $"Removing CubeBlock [{cubeBlock.EntityId.ToEntityIdFormat()}]");
            if (!ImportantBlocks.ContainsKey(cubeBlock.EntityId)) return;
            RemoveBlock(ImportantBlocks[cubeBlock.EntityId]);
        }

        private void RemoveBlock(Block block)
        {
            WriteGeneral(nameof(OnResetBlock), $"Removing Block [{block.IsReset.ToSingleChar()}] [{block.EntityId.ToEntityIdFormat()}]");
            if (!ImportantBlocks.ContainsKey(block.EntityId)) return;
            DeRegisterBlockEvents(block);
            RemoveFromDictionary(block);
            _gridController.Mediator.ReturnBlock(block, block.EntityId);
        }

        private void AddToDictionary(Block block)
        {
            if (block == null || !block.IsFunctional) return;
            //WriteGeneral(nameof(AddToDictionary), $"Adding block to Dictionary [{block.EntityId.ToEntityIdFormat()}] [{block.OwnerId.ToEntityIdFormat()}] [{_gridOwnership.RightfulOwner.ToEntityIdFormat()}] ");
            if (ImportantBlocks.ContainsKey(block.EntityId)) return;
            ImportantBlocks.Add(block.EntityId, block);
            if(block.IsFunctional && block.OwnerId != _gridController.GridOwnership.RightfulOwner)
                block.MyCubeBlock.ChangeOwner(_gridController.GridOwnership.RightfulOwner, MyOwnershipShareModeEnum.Faction);
            //WriteGeneral(nameof(AddToDictionary), $"Added block to Dictionary [{block.EntityId.ToEntityIdFormat()}] [{block.OwnerId.ToEntityIdFormat()}] [{_gridOwnership.RightfulOwner.ToEntityIdFormat()}] ");
        }

        private void RemoveFromDictionary(Block block)
        {
            if (block == null) return;
            //WriteGeneral(nameof(RemoveFromDictionary), $"Removing block from Dictionary [{block.EntityId.ToEntityIdFormat()}] [{block.OwnerId.ToEntityIdFormat()}] [{_gridOwnership.RightfulOwner.ToEntityIdFormat()}] ");
            if (!ImportantBlocks.ContainsKey(block.EntityId)) return;
            ImportantBlocks.Remove(block.EntityId);
            if (!block.IsFunctional)
                block.MyCubeBlock.ChangeOwner(0, MyOwnershipShareModeEnum.All);
            //if (ImportantBlocks.Count == 0)
            //    TriggerRequestGridGroupOwnershipEvaluation();
        }

        private void RegisterBlockEvents(Block block)
        {
            block.OnBlockIsNotWorking += BlockIsNotWorking;
            block.OnBlockIsWorking += BlockIsWorking;
            block.OnReset += OnResetBlock;
        }

        private void BlockIsNotWorking(Block block)
        {
            WriteGeneral(nameof(BlockIsNotWorking), $"Event invoked for {block.EntityId.ToEntityIdFormat()}");
            //RemoveFromDictionary(block);
            if (GridHasImportantBlocks(_gridController.ThisGrid)) return;
            _gridController.SetGridOwnership();
        }

        private void BlockIsWorking(Block block)
        {
            WriteGeneral(nameof(BlockIsWorking), $"Event invoked for {block.EntityId.ToEntityIdFormat()}");
            //AddToDictionary(block);
        }

        private void OnResetBlock(IReset reset)
        {
            Block block = (Block)reset;
            WriteGeneral(nameof(OnResetBlock), $"Reset triggered for [{block?.IsReset.ToSingleChar()}] [{block?.EntityId.ToEntityIdFormat()}]");
            if (block == null) return;
            RemoveBlock(block);
        }

        private void DeRegisterBlockEvents(Block block)
        {
            block.OnBlockIsNotWorking -= BlockIsNotWorking;
            block.OnBlockIsWorking -= BlockIsWorking;
            block.OnReset -= OnResetBlock;
        }

        public bool GridHasImportantBlocks(MyCubeGrid grid)
        {
            foreach (var block in grid.GetFatBlocks())
            {
                if (!block.IsFunctional) continue;
                var type = AssignBlock(block);
                if (type == BlockType.None) continue;
                return true;
            }
            return false;
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

        public override void Reset()
        {
            if (ImportantBlocks.Count == 0) return;
            base.Reset();
            foreach (var kvp in ImportantBlocks)
            {
                Block block = kvp.Value;
                DeRegisterBlockEvents(block);
                _gridController.Mediator.ReturnBlock(block, block.EntityId);
            }
            ImportantBlocks.Clear();
        }
    }
}