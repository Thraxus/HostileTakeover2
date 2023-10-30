using System;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;

namespace HostileTakeover2.Thraxus.Models.Loggers
{
    public class Block : IResetWithEvent<Block>
    {
        public event Action<Block> OnBlockIsNotWorking;
        public event Action<Block> OnBlockIsWorking;
        public event Action<Block> OnReset;

        public MyCubeBlock MyCubeBlock;
        private GridOwnership _gridOwnership;
        
        public BlockType BlockType;
        public string Name => MyCubeBlock.Name;
        public long EntityId;
        
        public bool IsReset { get; set; }
        public bool IsFunctional => MyCubeBlock.IsFunctional;
        public long OwnerId => MyCubeBlock.OwnerId;
        
        public void Init(BlockType blockType, MyCubeBlock block, GridOwnership gridOwnership)
        {
            IsReset = false;
            BlockType = blockType;
            MyCubeBlock = block;
            EntityId = block.EntityId;
            _gridOwnership = gridOwnership;
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            MyCubeBlock.OnClose += OnClose;
            MyCubeBlock.IsWorkingChanged += BlockOnWorkingChanged;
            ((IMyTerminalBlock)MyCubeBlock).OwnershipChanged += BlockOwnershipChanged;
        }

        private void OnClose(MyEntity entity)
        {
            var block = (MyCubeBlock)entity;
            OnReset?.Invoke(this);
        }

        private void DeRegisterEvents()
        {
            if (MyCubeBlock == null) return;
            MyCubeBlock.OnClose -= OnClose;
            MyCubeBlock.IsWorkingChanged -= BlockOnWorkingChanged;
            ((IMyTerminalBlock)MyCubeBlock).OwnershipChanged -= BlockOwnershipChanged;
        }

        private void BlockOwnershipChanged(IMyTerminalBlock block)
        {
            SetOwnership(MyCubeBlock);
        }

        private void SetOwnership(MyCubeBlock block)
        {
            block.ChangeOwner(block.IsFunctional ? _gridOwnership.RightfulOwner : 0, MyOwnershipShareModeEnum.None);
        }

        private void BlockOnWorkingChanged(MyCubeBlock block)
        {
            BlockOwnershipChanged((IMyTerminalBlock)block);
            switch (block.IsFunctional)
            {
                case false:
                    OnBlockIsNotWorking?.Invoke(this);
                    break;
                case true:
                    OnBlockIsWorking?.Invoke(this);
                    break;
            }
        }

        public void Reset()
        {
            IsReset = true;
            DeRegisterEvents();
            MyCubeBlock = null;
            BlockType = BlockType.None;
            EntityId = 0;
        }
    }
}