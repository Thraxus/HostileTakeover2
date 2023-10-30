using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    public class GridController : BaseLoggingClass, IReset
    {
        public MyCubeGrid ThisGrid;
        public IMyGridGroupData MyGridGroupData => ThisGrid.GetGridGroup(GridLinkTypeEnum.Logical);

        public readonly GridOwnership GridOwnership = new GridOwnership();
        public readonly BlockTypeController BlockTypeController = new BlockTypeController();

        public Mediator Mediator;
        public BaseGrid BaseGrid;

        public long EntityId;
        
         public bool IsReset { get; private set; }

        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            IsReset = false;
            OverrideLogPrefix(grid.EntityId.ToEntityIdFormat());
            WriteGeneral(nameof(Init), $"Primary Initialization starting.");
            ThisGrid = grid;
            ThisGrid.OnClose += CloseGrid;
            EntityId = grid.EntityId;
            Mediator = mediator;
            RegisterControllerEvents();
            Mediator.GridCollectionController.AddToGrids(ThisGrid.EntityId, this);
            BlockTypeController.Init(this);
            SetGridOwnership();
            WriteGeneral(nameof(Init), $"Primary Initialization complete.");
        }

        private void CloseGrid(MyEntity unused)
        {
            ThisGrid.OnClose -= CloseGrid;
            WriteGeneral(nameof(CloseGrid), $"Grid is closing, returning to Mediator.");
            Mediator.ReturnGridController(this, ThisGrid.EntityId);
        }

        public void Reset()
        {
            WriteGeneral(nameof(Reset), $"Starting Reset Cycle for Grid [{ThisGrid.EntityId:D18}]");
            DeRegisterControllerEvents();
            WriteGeneral(nameof(Reset), $"Reset1 complete, on to Reset2 for Grid [{ThisGrid.EntityId:D18}]");
            GridOwnership.Reset();
            Mediator.ReturnGrid(BaseGrid);
            BaseGrid = null;
            BlockTypeController.Reset();
            Mediator.GridCollectionController.RemoveFromGrids(ThisGrid.EntityId);
        }

        public void SetGridOwnership()
        {
            Mediator.GridGroupOwnershipTypeCoordinationController.SetGridGroupOwnership(MyGridGroupData);
        }

        public void SetGridOwnership(long ownerId, OwnerType ownerType)
        {
            GridOwnership.SetGridOwnership(ownerId, ThisGrid.EntityId, ownerType);
            SetOwner(ownerType);
        }
        
        public void SetOwner(OwnerType newOwnerType)
        {
            if (IsReset) return;
            WriteGeneral(nameof(SetOwner), $"Setting owner [{newOwnerType}] for Grid [{ThisGrid.EntityId:D18}] engaging.");
            if (BaseGrid != null)
            {
                Mediator.ReturnGrid(BaseGrid);
                BaseGrid = null;
            }

            if (newOwnerType == OwnerType.None)
            {
                DisownGrid();
            }

            BaseGrid = Mediator.GetGrid(newOwnerType);
            BaseGrid.Init(ThisGrid, this);
            WriteGeneral(nameof(SetOwner), $"Setting owner [{newOwnerType}] for Grid [{ThisGrid.EntityId:D18}] complete.");
        }

        private void DisownGrid()
        {
            foreach (var block in ThisGrid.GetFatBlocks())
            {
                if (!block.IsFunctional) continue;
                block.ChangeOwner(0, MyOwnershipShareModeEnum.All);
            }
        }

        private void RegisterControllerEvents()
        {
            BlockTypeController.OnWriteToLog += WriteGeneral;
        }

        private void DeRegisterControllerEvents()
        {
            BlockTypeController.OnWriteToLog -= WriteGeneral;
        }
    }
}