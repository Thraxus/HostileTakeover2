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
        private OwnershipController _ownershipController;

        public BlockType BlockType;
        
        public void Initialize(BlockType blockType, MyCubeBlock block, OwnershipController ownershipController)
        {
            BlockType = blockType;
            _block = block;
            _ownershipController = ownershipController;
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
            _ownershipController.SetOwnership(_block);
        }

        private void BlockOnWorkingChanged(MyCubeBlock block)
        {
            
        }
        
        public override void Close()
        {
            base.Close();
            DeRegisterEvents();
        }
    }
}