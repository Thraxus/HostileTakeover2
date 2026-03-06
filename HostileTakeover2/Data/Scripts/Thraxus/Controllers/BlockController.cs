using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Infrastructure;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Common.Utilities.Tools.Research;
using Sandbox.Game.Entities;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class BlockController : BaseLoggingClass
    {
        private readonly Dictionary<MyCubeBlock, Block> _importantBlocks =
            new Dictionary<MyCubeBlock, Block>();

        public Action OnImportantBlocksEmpty;

        private int _pendingAddCount;
        public bool HasPendingAdds => _pendingAddCount > 0;

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
            _pendingAddCount++;
            _mediator.ActionQueue.Add(10, () =>
            {
                try
                {
                    RuntimeBlockLogger.OnBlockEncountered?.Invoke(myCubeBlock);
                    var blockType = AssignBlock(myCubeBlock);
                    if (blockType == BlockType.None) return;
                    if (!myCubeBlock.IsFunctional) return;
                    if (_importantBlocks.ContainsKey(myCubeBlock)) return;
                    IsClosed = false;
                    Block block = _mediator.GetBlock(myCubeBlock.EntityId);
                    block.Initialize(blockType, myCubeBlock, _ownershipController);
                    if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Blocks))
                        WriteGeneral(DebugType.Blocks, nameof(AddBlock), $"Block classified [{blockType}]: {myCubeBlock.EntityId:D18}");
                    RegisterBlockEvents(block);
                    AddToDictionary(myCubeBlock, block);
                }
                finally
                {
                    _pendingAddCount--;
                    if (_pendingAddCount == 0 && _importantBlocks.Count == 0 && !IsClosed)
                        OnImportantBlocksEmpty?.Invoke();
                }
            });
        }

        private void AddToDictionary(MyCubeBlock myCubeBlock, Block block)
        {
            if (_importantBlocks.ContainsKey(myCubeBlock)) return;
            _importantBlocks.Add(myCubeBlock, block);
        }

        private void RemoveFromDictionary(MyCubeBlock myCubeBlock)
        {
            _importantBlocks.Remove(myCubeBlock);
        }

        private void RegisterBlockEvents(Block block)
        {
            block.OnReset += OnResetBlock;
            block.OnClose += BlockOnClose;
            block.BlockHasBeenDisabledAction += OnBlockDisabled;
        }

        private void BlockOnClose(IClose block)
        {
            var b = (Block)block;
            Block tracked;
            if (!_importantBlocks.TryGetValue(b.MyCubeBlock, out tracked)) return;
            OnResetBlock(tracked);
        }

        private void OnBlockDisabled(Block block)
        {
            if (!_importantBlocks.ContainsKey(block.MyCubeBlock)) return;
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Blocks))
                WriteGeneral(DebugType.Blocks, nameof(OnBlockDisabled), $"Block disabled [{block.BlockType}]: {block.EntityId:D18}");
            long entityId = block.EntityId;
            DeRegisterBlockEvents(block);
            RemoveFromDictionary(block.MyCubeBlock);
            _mediator.ActionQueue.Add(1, () => _mediator.ReturnBlock(block));
            if (_importantBlocks.Count == 0)
                OnImportantBlocksEmpty?.Invoke();
        }

        private void DeRegisterBlockEvents(Block block)
        {
            block.OnReset -= OnResetBlock;
            block.OnClose -= BlockOnClose;
            block.BlockHasBeenDisabledAction -= OnBlockDisabled;
        }

        private void OnResetBlock(IReset block)
        {
            DeRegisterBlockEvents((Block)block);
            RemoveFromDictionary(((Block)block).MyCubeBlock);
            _mediator.ReturnBlock((Block)block);
            if (_importantBlocks.Count == 0)
                OnImportantBlocksEmpty?.Invoke();
        }

        private void ResetBlock(MyCubeBlock myCubeBlock)
        {
            if (!_importantBlocks.ContainsKey(myCubeBlock)) return;
            OnResetBlock(_importantBlocks[myCubeBlock]);
        }

        private BlockType AssignBlock(MyCubeBlock block)
        {
            string key = block.BlockDefinition.Id.ToString();
            var data = _mediator.BlockClassificationData;
            if (data.ControlBlocks.Contains(key)) return BlockType.Control;
            if (data.MedicalBlocks.Contains(key))  return BlockType.Medical;
            if (data.WeaponBlocks.Contains(key))   return BlockType.Weapon;
            if (data.TrapBlocks.Contains(key))     return BlockType.Trap;
            return BlockType.None;
        }

        private void SoftReset()
        {
            _pendingAddCount = 0;
            IsClosed = true;
            foreach (var kvp in _importantBlocks)
            {
                Block block = kvp.Value;
                DeRegisterBlockEvents(block);
                _mediator.ReturnBlock(block);
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

        public int GetImportantBlockCount()
        {
            return _importantBlocks.Count;
        }
    }
}
