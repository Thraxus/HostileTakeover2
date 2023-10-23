using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridController : BaseLoggingClass
    {
        /// <summary>
        /// Systems required for different ownership types:
        /// None / Player
        /// - On ownership check, trigger Ignore Grid, which should set the following conditions
        /// - Event for OnBlockOwnershipChanged (grid registration)
        /// - BlockTypeController reset
        /// NPC - All systems engaged
        /// </summary>

        private MyCubeGrid _me;
        private IMyGridGroupData _myGridGroupData;

        public readonly GridOwnership GridOwnership = new GridOwnership();
        public readonly BlockTypeController BlockTypeController = new BlockTypeController();

        public Mediator Mediator;
        public BaseGrid BaseGrid;

        public long CurrentOwnerId => _me.BigOwners.Count != 0 ? _me.BigOwners[0] : 0;
        public long EntityId;

        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            OverrideLogPrefix(grid.EntityId.ToEntityIdFormat());
            WriteGeneral(nameof(Init), $"Primary Initialization for Grid [{grid.EntityId:D18}] starting.");
            _me = grid;
            EntityId = grid.EntityId;
            Mediator = mediator;
            RegisterControllerEvents();
            Mediator.GridCollectionController.AddToGrids(_me.EntityId, this);
            BlockTypeController.Init(Mediator, GridOwnership);
            SetGridGroupData();
            RegisterGridGroupDataEvents();
            WriteGeneral(nameof(Init), $"Primary Initialization for Grid [{_me.EntityId:D18}] complete.");
            Mediator.ActionQueue.Add(DefaultSettings.EntityAddTickDelay, DelayedInit);
        }

        private void SetGridGroupData()
        {
            WriteGeneral(nameof(SetGridGroupData), $"Setting up GridGroupData for [{(_myGridGroupData != null).ToSingleChar()}] [{_me.EntityId.ToEntityIdFormat()}].");
            _myGridGroupData = _me.GetGridGroup(GridLinkTypeEnum.Logical);
        }


        private void DelayedInit()
        {
            // Sequence! 
            // 1) Request Ownership Evaluation
            // 2) ...
            // 3) Profit!
            
            WriteGeneral(nameof(DelayedInit), $"Delayed Initialization for Grid [{_me.EntityId:D18}] starting.");
            GridOwnership.SetCurrentGridOwnership(CurrentOwnerId);
            SetGridOwnership();

            WriteGeneral(nameof(DelayedInit), $"Delayed Initialization for Grid [{_me.EntityId:D18}] complete.");
        }

        public void SetGridOwnership()
        {
            Mediator.GridGroupOwnershipCoordinationController.SetGridOwnership(_myGridGroupData);
        }
        
        public void SetOwner(OwnerType newOwnerType)
        {
            WriteGeneral(nameof(SetOwner), $"Setting owner for Grid [{_me.EntityId:D18}] complete.");
            if (BaseGrid != null)
            {
                DeRegisterGridEvents();
                Mediator.ReturnGrid(BaseGrid);
            }
            BaseGrid = Mediator.GetGrid(newOwnerType);
            BaseGrid.Init(_me, this);
            RegisterGridEvents();
        }

        public void TriggerHighlights(long grinderOwnerIdentityId)
        {
            if (!Mediator.DefaultSettings.UseHighlights.Current) return;
            Mediator.HighlightController.EnableHighlights(_myGridGroupData, grinderOwnerIdentityId);
        }

        private void RegisterControllerEvents()
        {
            BlockTypeController.OnWriteToLog += WriteGeneral;
            GridOwnership.OnWriteToLog += WriteGeneral;
        }

        private void DeRegisterControllerEvents()
        {
            BlockTypeController.OnWriteToLog += WriteGeneral;
            GridOwnership.OnWriteToLog -= WriteGeneral;
        }

        private void RegisterGridEvents()
        {
            if (BaseGrid == null) return;
            BaseGrid.OnWriteToLog += WriteGeneral;
        }

        private void DeRegisterGridEvents()
        {
            if (BaseGrid == null) return;
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
            WriteGeneral(nameof(OnGridAdded), $"Grid was added.  Adding to IMyGridGroupData for [{(_me.EntityId == newGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{newGrid.EntityId:D18}].");
            BlockTypeController.AddGrid((MyCubeGrid)newGrid);
        }

        private void OnGridRemoved(IMyGridGroupData thisGridGroup, IMyCubeGrid removedGrid, IMyGridGroupData newGridGroup)
        {
            WriteGeneral(nameof(OnGridRemoved), $"Grid was removed.  Resetting IMyGridGroupData for [{(_me.EntityId == removedGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{removedGrid.EntityId:D18}].");
            if (removedGrid == _me)
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
            base.Reset();
            DeRegisterControllerEvents();
            DeRegisterGridGroupDataEvents();
            DeRegisterGridEvents();
            GridOwnership.Reset();
            BlockTypeController.Reset();
            Mediator.GridCollectionController.RemoveFromGrids(_me.EntityId);
            Mediator.ReturnGridController(this, _me.EntityId);
        }

        //public void DisownGrid()
        //{
        //    //TODO This needs to be worked up from the ground up. SetOwnership should have called this if the 
        //    //TODO  grid group was once owned by a NPC and has now met the conditions of disownership, or if the 
        //    //TODO  grid group does not meet the requirements for ownership by a NPC.
        //    //TODO Certain systems do no need to do any work while this grid is not owned by a NPC
        //    //TODO The only things that need to be monitored while a grid is NOT owned by a NPC are: 
        //    //TODO  1) Ownership changes (MyCubeGrid.OnBlockOwnershipChanged)
        //    //TODO  2) ... nothing else?
        //    //TODO The GridOwnershipController and BlockTypeController should sit idle.
        //    //TODO Ownership must be calculated from the GridGroupData level
        //    //GridOwnershipController.Reset();
        //    //BlockTypeController.Reset();
        //    //SetOwnership();
        //}

        //private void IgnoreGrid()
        //{
        //    //TODO This needs to be worked up from the ground up. SetOwnership should have called this if the 
        //    //TODO  grid group was determined to be owned by a player, not owned, or called to be disowned by the GridGroupOwnershipTypeCoordinationController
        //    //TODO Certain systems do no need to do any work while this grid is not owned by a NPC
        //    //TODO The only things that need to be monitored while a grid is NOT owned by a NPC are: 
        //    //TODO  1) Ownership changes (MyCubeGrid.OnBlockOwnershipChanged)
        //    //TODO  2) ... nothing else?
        //    //TODO The GridOwnershipController and BlockTypeController should sit idle.
        //    //TODO Ownership must be calculated from the GridGroupData level
        //    SetEvents();
        //}

        //private void TakeOverGrid()
        //{
        //    WriteGeneral(nameof(TakeOverGrid), $"Attempting to take over grid: [{(!_mediator.DefaultSettings.CapturePlayerBlocks.Current).ToSingleChar()}] [{(GridOwnership.OwnershipType == OwnerType.Npc).ToSingleChar()}]");
        //    if (!_mediator.DefaultSettings.CapturePlayerBlocks.Current) return;
        //    if (GridOwnership.OwnershipType != OwnerType.Npc) return;
        //    //SetOwnership();
        //    SetEvents();
        //}

        //private void SetEvents()
        //{
        //    DeRegisterGridEvents();
        //    RegisterGridEvents();
        //}

        ////private void SetOwnership()
        ////{
        ////    BlockTypeController.AddGrid(_me);
        ////}





        //private void AddBlock(MyCubeBlock block)
        //{
        //    SetOwnership(block);
        //    BlockTypeController.AddBlock(block);
        //}

        //private void OnBlockAdded(MyCubeBlock block)
        //{
        //    // TODO need to check here for the connector being added from a player ship.  We shouldn't be taking that over.  At the same time, we don't want it connected either. 
        //    // TODO perhaps add logic that looks for store blocks on the NPC grid and if none found (or the mating connector has trade disabled?) then just unlatch the connectors
        //    var connector = block as IMyShipConnector;
        //    if (connector != null && connector.IsFunctional && connector.IsConnected) return; // maybe this works?  
        //    if (GridOwnership.OwnershipType == OwnerType.Npc)
        //        _mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => AddBlock(block));
        //}

        //private void OnBlockOwnershipChanged(MyCubeGrid unused)
        //{
        //    // This only needs to trigger if the grid is not owned by a NPC.  
        //    // The check only exists to see if we need to take over monitoring the grid or not.
        //    if (GridOwnership.OwnershipType != OwnerType.Npc)
        //        _mediator.GridGroupOwnerCoordinationController.InitializeOwnership(_myGridGroupData);
        //}

        //private void RequestGridGroupOwnershipEvaluation()
        //{
        //    _mediator.GridGroupOwnershipTypeCoordinationController.GridGroupOwnershipEvaluation(_myGridGroupData);
        //}

        //private void ReEvaluateOwnership()
        //{
        //    WriteGeneral(nameof(ReEvaluateOwnership), $"Reevaluating ownership.  Current Rightful Owner: [{GridOwnership.RightfulOwner.ToEntityIdFormat()}]");
        //    _mediator.GridGroupOwnerCoordinationController.ReEvaluateOwnership(_myGridGroupData, GridOwnership.RightfulOwner);
        //}

        //private void EvaluateOwnership()
        //{
        //    _mediator.GridGroupOwnerCoordinationController.InitializeOwnership(_myGridGroupData);
        //}

        //private void OnGridMerge(MyCubeGrid newGrid, MyCubeGrid oldGrid)
        //{
        //    WriteGeneral(nameof(OnGridMerge), $"Grid Merge -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
        //    BlockTypeController.AddGrid(oldGrid);
        //    SetGridGroupData();
        //}

        //private void OnGridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        //{
        //    WriteGeneral(nameof(OnGridSplit), $"Grid Split -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
        //    BlockTypeController.RemoveBlocks(newGrid);
        //}

        //private void OnMarkForClose(MyEntity myCubeGrid)
        //{
        //    Reset();
        //}


    }
}