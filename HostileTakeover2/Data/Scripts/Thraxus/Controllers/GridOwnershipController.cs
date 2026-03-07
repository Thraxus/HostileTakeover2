using System;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GridOwnershipController : BaseLoggingClass
    {
        public long RightfulOwner = 0;
        public OwnershipType OwnershipType = OwnershipType.None;

        private MyCubeGrid _me;
        private BlockController _blockController;
        private Func<long, bool> _isNpcIdentity;
        private bool _reclaimingBlocks;

        public void Init(MyCubeGrid me, BlockController blockController, Func<long, bool> isNpcIdentity)
        {
            _me = me;
            _blockController = blockController;
            _isNpcIdentity = isNpcIdentity;
        }

        public void SetOwnership(long rightfulOwner)
        {
            RightfulOwner = rightfulOwner;
            OwnershipType = rightfulOwner == 0 ? OwnershipType.None : _isNpcIdentity(rightfulOwner)
                ? OwnershipType.Npc : OwnershipType.Player;

            WriteGeneral(nameof(SetOwnership), $"Owner determined to be {rightfulOwner:D18} which is of type {OwnershipType}");

            switch (OwnershipType)
            {
                case OwnershipType.Npc:
                    TakeOverGrid();
                    break;
                case OwnershipType.Player:
                    break;
                case OwnershipType.None:
                default:
                    DisownGrid();
                    break;
            }
        }

        private void TakeOverGrid()
        {
            if (_blockController.IsClosed) return;
            WriteGeneral(nameof(TakeOverGrid), $"Taking over grid: [{_me.EntityId:D18}]");
            _blockController.AddGrid(_me);
        }

        public void DisownGrid()
        {
            _me.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
            _blockController.Reset();
            RightfulOwner = 0;
            OwnershipType = OwnershipType.None;
        }

        public void SetBlockOwnership(MyCubeBlock block)
        {
            block.ChangeOwner(block.IsFunctional ? RightfulOwner : 0, MyOwnershipShareModeEnum.Faction);
        }

        public void ReclaimHackedBlocks()
        {
            if (_reclaimingBlocks) return;
            try
            {
                _reclaimingBlocks = true;
                _me.ChangeGridOwnership(RightfulOwner, MyOwnershipShareModeEnum.Faction);
                _blockController.HandleNonFunctionalBlocks();
            }
            catch (Exception e) { WriteGeneral(nameof(ReclaimHackedBlocks), $"Exception: {e}"); }
            finally { _reclaimingBlocks = false; }
        }

        public override void Reset()
        {
            RightfulOwner = 0;
            OwnershipType = OwnershipType.None;
            _reclaimingBlocks = false;
            _me = null;
            _blockController = null;
            _isNpcIdentity = null;
            base.Reset();
        }
    }
}
