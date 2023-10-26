using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridController : BaseLoggingClass
    {
        public MyCubeGrid ThisGrid;
        private IMyGridGroupData _myGridGroupData;

        public readonly GridOwnership GridOwnership = new GridOwnership();
        public readonly BlockTypeController BlockTypeController = new BlockTypeController();

        public Mediator Mediator;
        public BaseGrid BaseGrid;

        public long EntityId;
        
        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            OverrideLogPrefix(grid.EntityId.ToEntityIdFormat());
            WriteGeneral(nameof(Init), $"Primary Initialization for Grid [{grid.EntityId:D18}] starting.");
            ThisGrid = grid;
            ThisGrid.OnClose += CloseGrid;
            EntityId = grid.EntityId;
            Mediator = mediator;
            RegisterControllerEvents();
            Mediator.GridCollectionController.AddToGrids(ThisGrid.EntityId, this);
            BlockTypeController.Init(this);
            SetGridGroupData();
            RegisterGridGroupDataEvents();
            WriteGeneral(nameof(Init), $"Primary Initialization for Grid [{ThisGrid.EntityId:D18}] complete.");
            Mediator.ActionQueue.Add(DefaultSettings.EntityAddTickDelay, DelayedInit);
        }

        private void CloseGrid(MyEntity unused)
        {
            ThisGrid.OnClose -= CloseGrid;
            Reset();
        }

        private void SetGridGroupData()
        {
            _myGridGroupData = ThisGrid.GetGridGroup(GridLinkTypeEnum.Logical);
            WriteGeneral(nameof(SetGridGroupData), $"Setting up GridGroupData for [{(_myGridGroupData != null).ToSingleChar()}] [{ThisGrid.EntityId.ToEntityIdFormat()}].");
        }
        
        private void DelayedInit()
        {
            // Sequence! 
            // 1) Request Ownership Evaluation
            // 2) ...
            // 3) Profit!
            
            WriteGeneral(nameof(DelayedInit), $"Delayed Initialization for Grid [{ThisGrid.EntityId:D18}] starting.");
            //GridOwnership.SetCurrentGridOwnership(CurrentOwnerId, _me.EntityId);
            SetGridOwnership();

            WriteGeneral(nameof(DelayedInit), $"Delayed Initialization for Grid [{ThisGrid.EntityId:D18}] complete.");
        }

        public void SetGridOwnership()
        {
            Mediator.GridGroupOwnershipTypeCoordinationController.SetGridGroupOwnership(_myGridGroupData);
        }

        public void SetGridOwnership(long ownerId, OwnerType ownerType)
        {
            GridOwnership.SetGridOwnership(ownerId, ThisGrid.EntityId, ownerType);
            SetOwner(ownerType);
        }
        
        public void SetOwner(OwnerType newOwnerType)
        {
            WriteGeneral(nameof(SetOwner), $"Setting owner [{newOwnerType}] for Grid [{ThisGrid.EntityId:D18}] engaging.");
            if (BaseGrid != null)
            {
                DeRegisterGridEvents();
                Mediator.ReturnGrid(BaseGrid);
                BaseGrid = null;
            }

            if (newOwnerType == OwnerType.None)
            {
                DisownGrid();
            }

            BaseGrid = Mediator.GetGrid(newOwnerType);
            BaseGrid.Init(ThisGrid, this);
            RegisterGridEvents();
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

        public void TriggerHighlights(long grinderOwnerIdentityId)
        {
            if (!Mediator.DefaultSettings.UseHighlights.Current) return;
            Mediator.HighlightController.EnableHighlights(_myGridGroupData, grinderOwnerIdentityId);
        }

        private void RegisterControllerEvents()
        {
            BlockTypeController.OnWriteToLog += WriteGeneral;
        }

        private void DeRegisterControllerEvents()
        {
            BlockTypeController.OnWriteToLog -= WriteGeneral;
        }

        private void RegisterGridEvents()
        {
            WriteGeneral(nameof(RegisterGridEvents), $"Registering Grid Events for Grid [{ThisGrid.EntityId:D18}] engaging.");
            BaseGrid.OnWriteToLog += WriteGeneral;
        }

        private void DeRegisterGridEvents()
        {
            WriteGeneral(nameof(DeRegisterGridEvents), $"DeRegistering Grid Events for Grid [{ThisGrid.EntityId:D18}] engaging.");
            BaseGrid.OnWriteToLog -= WriteGeneral;
        }

        private void RegisterGridGroupDataEvents()
        {
            _myGridGroupData.OnGridRemoved += OnGridRemoved;
            _myGridGroupData.OnGridAdded += OnGridAdded;
            _myGridGroupData.OnReleased += GridGroupReleased;
        }

        private void DeRegisterGridGroupDataEvents()
        {
            _myGridGroupData.OnReleased -= GridGroupReleased;
            _myGridGroupData.OnGridAdded -= OnGridAdded;
            _myGridGroupData.OnGridRemoved -= OnGridRemoved;
        }

        private void OnGridAdded(IMyGridGroupData newGridGroup, IMyCubeGrid newGrid, IMyGridGroupData oldGridGroup)
        {
            WriteGeneral(nameof(OnGridAdded), $"Grid was added.  Adding to IMyGridGroupData for [{(ThisGrid.EntityId == newGrid.EntityId).ToSingleChar()}] [{ThisGrid.EntityId:D18}] [{newGrid.EntityId:D18}].");
            BlockTypeController.AddGrid((MyCubeGrid)newGrid);
        }

        private void OnGridRemoved(IMyGridGroupData thisGridGroup, IMyCubeGrid removedGrid, IMyGridGroupData newGridGroup)
        {
            WriteGeneral(nameof(OnGridRemoved), $"Grid was removed.  Resetting IMyGridGroupData for [{(ThisGrid.EntityId == removedGrid.EntityId).ToSingleChar()}] [{ThisGrid.EntityId:D18}] [{removedGrid.EntityId:D18}].");
            if (removedGrid == ThisGrid)
            {
                Reset();
                return;
            }
            BlockTypeController.RemoveGrid((MyCubeGrid)removedGrid);
        }

        private void GridGroupReleased(IMyGridGroupData myGridGroupData)
        {
            Reset();
        }

        public override void Reset()
        {
            WriteGeneral(nameof(Reset), $"Starting Reset Cycle for Grid [{ThisGrid.EntityId:D18}]");
            base.Reset();
            DeRegisterControllerEvents();
            DeRegisterGridGroupDataEvents();
            DeRegisterGridEvents();
            WriteGeneral(nameof(Reset), $"Reset1 complete, on to Reset2 for Grid [{ThisGrid.EntityId:D18}]");
            Reset2();
        }

        private void Reset2()
        {
            WriteGeneral(nameof(Reset), $"Starting Reset2 for Grid [{ThisGrid.EntityId:D18}]");
            GridOwnership.Reset();
            Mediator.ReturnGrid(BaseGrid);
            BaseGrid = null;
            BlockTypeController.Reset();
            Mediator.GridCollectionController.RemoveFromGrids(ThisGrid.EntityId);
            WriteGeneral(nameof(Reset), $"Reset2 complete, on to Reset3 for Grid [{ThisGrid.EntityId:D18}]");
            Reset3();
        }

        private void Reset3()
        {
            WriteGeneral(nameof(Reset), $"Finalizing Reset Cycle for Grid [{ThisGrid.EntityId:D18}]");
            Mediator.ReturnGridController(this, ThisGrid.EntityId);
        }
    }
}