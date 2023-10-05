using System;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GridOwnershipController
    {
        public long RightfulOwner = 0;
        public OwnershipType OwnershipType = OwnershipType.None;

        public Action<MyCubeBlock> SetOwnershipAction;
        public Action DisownGridAction;
        public Action IgnoreGridAction;
        public Action TakeOverGridAction;
        
        private void DisownGrid()
        {
            DisownGridAction?.Invoke();
        }

        private void IgnoreGrid()
        {
            IgnoreGridAction?.Invoke();
        }

        private void TakeOverGrid()
        {
            TakeOverGridAction?.Invoke();
        }

        public void SetOwnership(MyCubeBlock block)
        {
            SetOwnershipAction?.Invoke(block);
        }

        public void SetOwnership(long rightfulOwner)
        {
            RightfulOwner = rightfulOwner;
            OwnershipType = rightfulOwner == 0 ? OwnershipType.None : MyAPIGateway.Players.TryGetSteamId(rightfulOwner) <= 0
                ? OwnershipType.Npc : OwnershipType.Player;
            
            switch (OwnershipType)
            {
                case OwnershipType.Npc:
                    TakeOverGrid();
                    break;
                case OwnershipType.Player:
                    IgnoreGrid();
                    break;
                case OwnershipType.None:
                default:
                    DisownGrid();
                    break;
            }
        }

        public void SoftReset()
        {
            RightfulOwner = 0;
            OwnershipType = OwnershipType.None;
        }

        public void Reset()
        {
            RightfulOwner = 0;
            SetOwnershipAction = null;
            OwnershipType = OwnershipType.None;
        }
    }
}