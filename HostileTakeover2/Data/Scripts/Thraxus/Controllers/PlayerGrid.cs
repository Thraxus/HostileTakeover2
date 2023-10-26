using Sandbox.Game.Entities;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class PlayerGrid : BaseGrid
    {
        public override void RegisterEvents()
        {
            ThisGrid.OnBlockOwnershipChanged += BlockOwnershipChanged;
        }

        private void BlockOwnershipChanged(MyCubeGrid grid)
        {
            if (ThisGrid.BigOwners != null && ThisGrid.BigOwners[0] == OwnerId) return;
            ThisGridController.SetGridOwnership();
        }

        public override void DeRegisterEvents()
        {
            ThisGrid.OnBlockOwnershipChanged -= BlockOwnershipChanged;
        }
    }
}