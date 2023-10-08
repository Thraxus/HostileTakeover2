using System;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Controllers.Loggers;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Models.Loggers
{
    internal class Block : BaseLoggingClass
    {
        public MyCubeBlock MyCubeBlock;
        private GridOwnershipController _gridOwnershipController;
        public Action<Block> BlockHasBeenDisableAction;
        public BlockType BlockType;
        public string Name => MyCubeBlock.Name;
        public long EntityId => MyCubeBlock.EntityId;

        public bool IsFunctional => MyCubeBlock.IsFunctional;
        
        public void Initialize(BlockType blockType, MyCubeBlock block, GridOwnershipController gridOwnershipController)
        {
            BlockType = blockType;
            MyCubeBlock = block;
            _gridOwnershipController = gridOwnershipController;
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            MyCubeBlock.OnClose += block => Close();
            MyCubeBlock.IsWorkingChanged += BlockOnWorkingChanged;
            ((IMyTerminalBlock)MyCubeBlock).OwnershipChanged += BlockOnOwnershipChanged;
        }

        private void DeRegisterEvents()
        {
            MyCubeBlock.IsWorkingChanged -= BlockOnWorkingChanged;
            ((IMyTerminalBlock)MyCubeBlock).OwnershipChanged -= BlockOnOwnershipChanged;
        }

        private void BlockOnOwnershipChanged(IMyTerminalBlock block)
        {
            _gridOwnershipController.SetOwnership(MyCubeBlock);
        }

        private void BlockOnWorkingChanged(MyCubeBlock block)
        {
            BlockOnOwnershipChanged((IMyTerminalBlock)block);
            if (!block.IsFunctional)
                BlockHasBeenDisable();
        }

        private void BlockHasBeenDisable()
        {
            BlockHasBeenDisableAction?.Invoke(this);
        }

        public override void Reset()
        {
            base.Reset();
            DeRegisterEvents();
            MyCubeBlock = null;
            BlockType = BlockType.None;
        }
    }
}