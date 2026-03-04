using System;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace HostileTakeover2.Thraxus.Models
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

        private void OnCubeBlockClose(IMyEntity entity) => Close();

        private void RegisterEvents()
        {
            MyCubeBlock.OnClose += OnCubeBlockClose;
            MyCubeBlock.IsWorkingChanged += BlockOnWorkingChanged;
            ((IMyTerminalBlock)MyCubeBlock).OwnershipChanged += BlockOnOwnershipChanged;
        }

        private void DeRegisterEvents()
        {
            MyCubeBlock.OnClose -= OnCubeBlockClose;
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