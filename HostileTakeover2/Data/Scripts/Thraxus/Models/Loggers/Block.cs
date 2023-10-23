﻿using System;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;

namespace HostileTakeover2.Thraxus.Models.Loggers
{
    internal class Block : IResetWithEvent
    {
        public event Action<Block> OnBlockIsNotWorking;
        public event Action<Block> OnBlockIsWorking;

        public MyCubeBlock MyCubeBlock;
        private GridOwnership _gridOwnership;
        
        public BlockType BlockType;
        public string Name => MyCubeBlock.Name;
        public long EntityId;

        public bool IsFunctional => MyCubeBlock.IsFunctional; 
        
        public void Init(BlockType blockType, MyCubeBlock block, GridOwnership gridOwnershipController)
        {
            BlockType = blockType;
            MyCubeBlock = block;
            EntityId = block.EntityId;
            _gridOwnership = gridOwnershipController;
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            MyCubeBlock.OnClose += Reset;
            MyCubeBlock.IsWorkingChanged += BlockOnWorkingChanged;
            ((IMyTerminalBlock)MyCubeBlock).OwnershipChanged += BlockOwnershipChanged;
        }

        private void DeRegisterEvents()
        {
            MyCubeBlock.OnClose -= Reset;
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
        
        private void Reset(MyEntity unused)
        {
            Reset();
        }

        public void Reset()
        {
            DeRegisterEvents();
            MyCubeBlock = null;
            BlockType = BlockType.None;
            EntityId = 0;
            OnReset?.Invoke(this);
        }

        public event Action<IResetWithEvent> OnReset;
    }
}