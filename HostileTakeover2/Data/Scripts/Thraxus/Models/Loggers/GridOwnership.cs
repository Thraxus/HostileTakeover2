using HostileTakeover2.Thraxus.Enums;

namespace HostileTakeover2.Thraxus.Models.Loggers
{
    public class GridOwnership
    {
        public long GridId;
        public long RightfulOwner = 0;
        public OwnerType OwnerType = OwnerType.NotEvaluated;

        public void SetGridOwnership(long ownerId, long gridId, OwnerType ownerType)
        {
            GridId = gridId;
            RightfulOwner = ownerType == OwnerType.None ? 0 : ownerId;
            OwnerType = ownerType;
        }

        public void Reset()
        {
            RightfulOwner = 0;
            OwnerType = OwnerType.NotEvaluated;
        }
    }
}