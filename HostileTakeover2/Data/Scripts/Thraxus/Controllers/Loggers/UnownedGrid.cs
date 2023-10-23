using Sandbox.Game.Entities;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class UnownedGrid : BaseGrid
    {
        public override void RegisterEvents()
        {
            ThisGrid.OnBlockOwnershipChanged += BlockOwnershipChanged;
        }

        private void BlockOwnershipChanged(MyCubeGrid grid)
        {
            ThisGridController.SetGridOwnership();
        }

        public override void DeRegisterEvents()
        {
            ThisGrid.OnBlockOwnershipChanged -= BlockOwnershipChanged;
        }
    }
}