using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers.Loggers;
using Sandbox.Game.Entities;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal abstract class BaseGrid : BaseLoggingClass, IHaveEvents, IInit<MyCubeGrid, GridController>
    {
        protected MyCubeGrid ThisGrid;
        protected GridController ThisGridController;
        protected long OwnerId;

        public virtual void Init(MyCubeGrid myCubeGrid, GridController gridController)
        {
            ThisGrid = myCubeGrid;
            ThisGridController = gridController;
            OwnerId = ThisGrid.BigOwners == null ? 0 : ThisGrid.BigOwners[0];
            RegisterEvents();
        }

        public abstract void RegisterEvents();

        public abstract void DeRegisterEvents();

        public override void Reset()
        {
            base.Reset();
            DeRegisterEvents();
            ThisGrid = null;
            ThisGridController = null;
            OwnerId = 0;
        }
    }
}