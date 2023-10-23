using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using Sandbox.Game.Entities;
using VRage.Game.Entity;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class BaseGrid : BaseLoggingClass, IHaveEvents, IInit<MyCubeGrid, GridController>
    {
        protected MyCubeGrid ThisGrid;
        protected GridController ThisGridController;
        protected BlockTypeController BlockTypeController;

        public void Init(MyCubeGrid myCubeGrid, GridController gridController)
        {
            ThisGrid = myCubeGrid;
            ThisGridController = gridController;
            BlockTypeController = ThisGridController.BlockTypeController;
            RegisterEvents();
        }

        public virtual void RegisterEvents()
        {
            WriteGeneral(nameof(RegisterEvents), $"Registering Events for a BaseGrid");
            ThisGrid.OnMarkForClose += MarkedForClose;
        }

        private void MarkedForClose(MyEntity unused)
        {
            ThisGridController.Reset();
        }

        public virtual void DeRegisterEvents()
        {
            ThisGrid.OnMarkForClose -= MarkedForClose;
        }

        public override void Reset()
        {
            DeRegisterEvents();
            ThisGrid = null;
            ThisGridController = null;
            BlockTypeController = null;
            base.Reset();
        }
    }
}