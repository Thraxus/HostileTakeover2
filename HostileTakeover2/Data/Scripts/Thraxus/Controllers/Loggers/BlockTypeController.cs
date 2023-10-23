using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
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
        public event Action RequestGridGroupOwnershipEvaluation;
        private void TriggerRequestGridGroupOwnershipEvaluation()
        {
            RequestGridGroupOwnershipEvaluation?.Invoke();
        }
        
        public readonly Dictionary<long, Block> EnabledImportantBlocks = new Dictionary<long, Block>();
        public Dictionary<long, Block> GetImportantBlockDictionary() => EnabledImportantBlocks;

        private Mediator _mediator;
        private GridOwnership _gridOwnership;
        
        public void Init(Mediator mediator, GridOwnership gridOwnership)
        {
            _mediator = mediator;
            _gridOwnership = gridOwnership;
        }

        public void AddGrid(MyCubeGrid grid)
        {
            WriteGeneral(nameof(AddGrid), $"Adding grid! [{grid.EntityId.ToEntityIdFormat()}]");
            foreach (var fatBlock in grid.GetFatBlocks())
            {
                AddBlock(fatBlock);
            }

            if (EnabledImportantBlocks.Count == 0)
                TriggerRequestGridGroupOwnershipEvaluation();
        }

        public void RemoveGrid(MyCubeGrid grid)
        {
            foreach (var fatBlock in grid.GetFatBlocks())
            {
                RemoveBlock(fatBlock);
            }

            if (EnabledImportantBlocks.Count == 0)
                TriggerRequestGridGroupOwnershipEvaluation();
        }
        
        public void AddBlock(MyCubeBlock myCubeBlock)
        {
            _mediator.ActionQueue.Add(10, () =>
            {
                var blockType = AssignBlock(myCubeBlock);
                if (blockType == BlockType.None) return;
                if (EnabledImportantBlocks.ContainsKey(myCubeBlock.EntityId)) return;
                Block block = _mediator.GetBlock(myCubeBlock.EntityId);
                block.Init(blockType, myCubeBlock, _gridOwnership);
                RegisterBlockEvents(block);
                if(block.IsFunctional)
                    AddToDictionary(block);
            });
        }

        private void RemoveBlock(MyCubeBlock myCubeBlock)
        {
            if (!EnabledImportantBlocks.ContainsKey(myCubeBlock.EntityId)) return;
            Block block = EnabledImportantBlocks[myCubeBlock.EntityId];
            DeRegisterBlockEvents(block);
            RemoveFromDictionary(block);
            _mediator.ReturnBlock(block, block.EntityId);
        }

        private void AddToDictionary(Block block)
        {
            if (EnabledImportantBlocks.ContainsKey(block.EntityId)) return;
            EnabledImportantBlocks.Add(block.EntityId, block);
        }

        private void RemoveFromDictionary(Block block)
        {
            if (EnabledImportantBlocks.ContainsKey(block.EntityId)) return;
            EnabledImportantBlocks.Remove(block.EntityId);
            if (EnabledImportantBlocks.Count == 0)
                TriggerRequestGridGroupOwnershipEvaluation();
        }

        private void RegisterBlockEvents(Block block)
        {
            block.OnBlockIsNotWorking += BlockIsNotWorking;
            block.OnBlockIsWorking += BlockIsWorking;
            block.OnReset += OnResetBlock;
        }

        private void BlockIsNotWorking(Block block)
        {
            WriteGeneral(nameof(BlockIsNotWorking), $"{nameof(BlockIsNotWorking)} event invoked for {block.EntityId.ToEntityIdFormat()}");
            RemoveFromDictionary(block);
        }

        private void BlockIsWorking(Block block)
        {
            WriteGeneral(nameof(BlockIsWorking), $"{nameof(BlockIsWorking)} event invoked for {block.EntityId.ToEntityIdFormat()}");
            AddToDictionary(block);
        }

        private void OnResetBlock(IReset reset)
        {
            MyCubeBlock block = ((Block)reset).MyCubeBlock;
            if (block == null) return;
            RemoveBlock(block);
        }

        private void DeRegisterBlockEvents(Block block)
        {
            block.OnBlockIsNotWorking -= BlockIsNotWorking;
            block.OnBlockIsWorking -= BlockIsWorking;
            block.OnReset -= OnResetBlock;
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
            base.Reset();
            foreach (var kvp in EnabledImportantBlocks)
            {
                Block block = kvp.Value;
                DeRegisterBlockEvents(block);
                _mediator.ReturnBlock(block, block.EntityId);
            }
            EnabledImportantBlocks.Clear();
        }
    }
}