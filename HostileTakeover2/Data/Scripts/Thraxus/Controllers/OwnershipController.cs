using System;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.Game.Entities;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class OwnershipController
    {
        public long OwnerId(MyCubeGrid grid) => grid.BigOwners.Count == 0 ? 0 : grid.BigOwners[0];
        public long RightfulOwner = 0;
        public OwnershipType OwnershipType = OwnershipType.Other;

        public Action<MyCubeBlock> SetOwnershipAction;
        
        public void SetOwnership(MyCubeBlock block)
        {
            SetOwnershipAction?.Invoke(block);
        }

        public void SetOwnership(long rightfulOwner, OwnershipType ownershipType)
        {
            RightfulOwner = rightfulOwner;
            OwnershipType = ownershipType;
        }

        public void SoftReset()
        {
            RightfulOwner = 0;
            OwnershipType = OwnershipType.Other;
        }

        public void Reset()
        {
            RightfulOwner = 0;
            SetOwnershipAction = null;
            OwnershipType = OwnershipType.Other;
        }
    }
}