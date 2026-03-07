using System;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using VRage.ModAPI;

namespace HostileTakeover2.Thraxus.Models
{
    internal class Block : BaseLoggingClass
    {
        public MyCubeBlock MyCubeBlock;
        public Action<Block> BlockHasBeenDisabledAction;
        public BlockType BlockType;
        public string Name => MyCubeBlock?.Name ?? string.Empty;
        public long EntityId { get; private set; }

        public bool IsFunctional => MyCubeBlock.IsFunctional;

        public void Initialize(BlockType blockType, MyCubeBlock block)
        {
            BlockType = blockType;
            MyCubeBlock = block;
            EntityId = block.EntityId;
            RegisterEvents();
        }

        private void OnCubeBlockClose(IMyEntity entity)
        {
            try { Close(); }
            catch (Exception e) { WriteGeneral(nameof(OnCubeBlockClose), $"Exception: {e}"); }
        }

        private void RegisterEvents()
        {
            MyCubeBlock.OnClose += OnCubeBlockClose;
            MyCubeBlock.IsWorkingChanged += BlockOnWorkingChanged;
        }

        private void DeRegisterEvents()
        {
            MyCubeBlock.OnClose -= OnCubeBlockClose;
            MyCubeBlock.IsWorkingChanged -= BlockOnWorkingChanged;
        }

        private void BlockOnWorkingChanged(MyCubeBlock block)
        {
            try
            {
                // IsWorkingChanged fires on both transitions (working → broken AND broken → working).
                // We only care when the block stops being functional (i.e. getting ground down).
                if (!block.IsFunctional)
                    BlockHasBeenDisabled();
            }
            catch (Exception e) { WriteGeneral(nameof(BlockOnWorkingChanged), $"Exception: {e}"); }
        }

        private void BlockHasBeenDisabled()
        {
            BlockHasBeenDisabledAction?.Invoke(this);
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