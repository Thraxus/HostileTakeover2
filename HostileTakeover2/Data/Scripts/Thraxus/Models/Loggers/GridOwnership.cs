using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Enums;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Models.Loggers
{
    internal class GridOwnership : BaseLoggingClass
    {
        public long RightfulOwner = 0;
        public OwnerType OwnershipType = OwnerType.NotEvaluated;

        public void SetCurrentGridOwnership(long ownerId)
        {
            OwnershipType = ownerId == 0 ? OwnerType.None : MyAPIGateway.Players.TryGetSteamId(ownerId) <= 0
                ? OwnerType.Npc : OwnerType.Player;
            
            WriteGeneral(nameof(SetCurrentGridOwnership), $"Owner determined to be {ownerId:D18} which is of type {OwnershipType}");
        }

        public bool HasOwnerChanged(long ownerId)
        {
            return RightfulOwner == ownerId;
        }

        public bool UpdateOwner(long newOwnerId)
        {
            if (!HasOwnerChanged(newOwnerId)) return false;
            SetCurrentGridOwnership(newOwnerId);
            return true;
        }

        public override void Reset()
        {
            RightfulOwner = 0;
            OwnershipType = OwnerType.NotEvaluated;
            base.Reset();
        }
    }
}