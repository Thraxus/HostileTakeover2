using System;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Models
{
    internal class Block : BaseLoggingClass
    {
        private MyCubeBlock _block;
        private GridOwnershipController _gridOwnershipController;
        public Action<Block> BlockHasBeenDisableAction;
        public BlockType BlockType;
        public string Name => _block.Name;

        public bool IsFunctional => _block.IsFunctional;
        
        public void Initialize(BlockType blockType, MyCubeBlock block, GridOwnershipController gridOwnershipController)
        {
            BlockType = blockType;
            _block = block;
            _gridOwnershipController = gridOwnershipController;
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            _block.OnClose += block => Close();
            _block.IsWorkingChanged += BlockOnWorkingChanged;
            ((IMyTerminalBlock)_block).OwnershipChanged += BlockOnOwnershipChanged;
        }

        private void DeRegisterEvents()
        {
            _block.IsWorkingChanged -= BlockOnWorkingChanged;
            ((IMyTerminalBlock)_block).OwnershipChanged -= BlockOnOwnershipChanged;
        }

        private void BlockOnOwnershipChanged(IMyTerminalBlock block)
        {
            _gridOwnershipController.SetOwnership(_block);
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

        public override void Close()
        {
            base.Close();
            DeRegisterEvents();
        }
    }
}