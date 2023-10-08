using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Controllers.Loggers;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Models.Loggers
{
    internal class Grid : BaseLoggingClass
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

        public readonly GridOwnershipController GridOwnershipController = new GridOwnershipController();
        public readonly BlockTypeController BlockTypeController = new BlockTypeController();
        private Mediator _mediator;
        private bool _initialized;

        public long CurrentOwnerId => _me.BigOwners.Count != 0 ? _me.BigOwners[0] : 0;
        public long EntityId => _me?.EntityId ?? 0;

        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            SetLogPrefix(grid.EntityId.ToEntityIdFormat());
            WriteGeneral(nameof(Init), $"Primary Initialization for Grid [{grid.EntityId:D18}] starting.");
            IsClosed = false;
            _initialized = true;
            _me = grid;
            _mediator = mediator;
            WriteGeneral(nameof(Init), $"Primary Initialization for Grid [{_me.EntityId:D18}] complete.");
            Init();
        }

        private void Init()
        {
            WriteGeneral(nameof(Init), $"Secondary Initialization for Grid [{_me.EntityId:D18}] starting.");
            _mediator.GridCollectionController.AddToGrids(_me.EntityId, this);
            BlockTypeController.OnWriteToLog += WriteGeneral;
            BlockTypeController.Init(_mediator, GridOwnershipController);
            GridOwnershipController.OnWriteToLog += WriteGeneral;
            GridOwnershipController.SetOwnershipAction += SetOwnership;
            GridOwnershipController.DisownGridAction += DisownGrid;
            GridOwnershipController.TakeOverGridAction += TakeOverGrid;
            GridOwnershipController.IgnoreGridAction += IgnoreGrid;
            SetupGridGroup();
            WriteGeneral(nameof(Init), $"Secondary Initialization for Grid [{_me.EntityId:D18}] complete.");
        }

        private void SetupGridGroup()
        {
            if (IsClosed) return;
            SetGridGroupData();
            EvaluateOwnership();
        }

        private void SetGridGroupData()
        {
            WriteGeneral(nameof(SetGridGroupData), $"Setting up GridGroupData for [{(_myGridGroupData != null).ToSingleChar()}] [{_me.EntityId.ToEntityIdFormat()}].");
            if (_myGridGroupData != null) DeRegisterGridGroupDataEvents(_myGridGroupData);
            _myGridGroupData = _me.GetGridGroup(GridLinkTypeEnum.Logical);
            if (_myGridGroupData == null)
            {
                WriteGeneral(nameof(SetGridGroupData), $"Rejecting further setup for [{_me.EntityId.ToEntityIdFormat()}] -- [{(_myGridGroupData == null).ToSingleChar()}] MyGridGroupData was null.");
                return;
            }
            RegisterGridGroupDataEvents();
        }

        private void ReEvaluateOwnership()
        {
            WriteGeneral(nameof(ReEvaluateOwnership), $"Reevaluating ownership.  Current Rightful Owner: [{GridOwnershipController.RightfulOwner.ToEntityIdFormat()}]");
            _mediator.GridGroupCoordinationController.ReEvaluateOwnership(_myGridGroupData, GridOwnershipController.RightfulOwner);
        }

        private void EvaluateOwnership()
        {
            _mediator.GridGroupCoordinationController.InitializeOwnership(_myGridGroupData);
        }

        private void RegisterGridGroupDataEvents()
        {
            _myGridGroupData.OnGridRemoved += OnGridRemoved;
            _myGridGroupData.OnGridAdded += OnGridAdded;
            _myGridGroupData.OnReleased += DeRegisterGridGroupDataEvents;
        }

        private void DeRegisterGridGroupDataEvents(IMyGridGroupData myGridGroupData)
        {
            myGridGroupData.OnReleased -= DeRegisterGridGroupDataEvents;
            _myGridGroupData.OnGridAdded -= OnGridAdded;
            myGridGroupData.OnGridRemoved -= OnGridRemoved;
        }

        private void OnGridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        {
            WriteGeneral(nameof(OnGridSplit), $"Grid Split -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
            BlockTypeController.RemoveOldBlocks(newGrid);
            ReEvaluateOwnership();
        }

        private void OnGridMerge(MyCubeGrid newGrid, MyCubeGrid oldGrid)
        {
            WriteGeneral(nameof(OnGridMerge), $"Grid Merge -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
            BlockTypeController.AddGrid(oldGrid);
            SetGridGroupData();
        }

        private void OnGridRemoved(IMyGridGroupData thisGridGroup, IMyCubeGrid removedGrid, IMyGridGroupData newGridGroup)
        { 
            WriteGeneral(nameof(OnGridRemoved), $"Grid was removed.  Resetting IMyGridGroupData for [{(_me.EntityId == removedGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{removedGrid.EntityId:D18}].");
            if (removedGrid == _me)
            {
                _mediator.ReturnGrid(this, _me.EntityId);
                return;
            }
            SetGridGroupData();
        }

        private void OnGridAdded(IMyGridGroupData newGridGroup, IMyCubeGrid newGrid, IMyGridGroupData oldGridGroup)
        {
            WriteGeneral(nameof(OnGridAdded), $"Grid was added.  Adding to IMyGridGroupData for [{(_me.EntityId == newGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{newGrid.EntityId:D18}].");
            BlockTypeController.AddGrid((MyCubeGrid)newGrid);
        }

        public void DisownGrid()
        {
            GridOwnershipController.Reset();
            BlockTypeController.Reset();
            SetOwnership();
        }

        private void IgnoreGrid()
        {
            SetEvents();
        }

        private void TakeOverGrid()
        {
            WriteGeneral(nameof(TakeOverGrid), $"Attempting to take over grid: [{(!_mediator.DefaultSettings.CapturePlayerBlocks.Current).ToSingleChar()}] [{(GridOwnershipController.OwnershipType == OwnershipType.Npc).ToSingleChar()}] [{(!BlockTypeController.IsClosed).ToSingleChar()}]");
            if (!_mediator.DefaultSettings.CapturePlayerBlocks.Current) return;
            if (GridOwnershipController.OwnershipType != OwnershipType.Npc || BlockTypeController.IsClosed) return;
            SetOwnership();
            SetEvents();
        }

        private void SetEvents()
        {
            DeRegisterEvents();
            RegisterEvents();
        }

        private void SetOwnership()
        {
            BlockTypeController.AddGrid(_me);
        }

        private void SetOwnership(MyCubeBlock block)
        {
            block.ChangeOwner(block.IsFunctional ? GridOwnershipController.RightfulOwner : 0, MyOwnershipShareModeEnum.None);
        }

        public void TriggerHighlights(long grinderOwnerIdentityId)
        {
            if (!_mediator.DefaultSettings.UseHighlights.Current) return;
            _mediator.HighlightController.EnableHighlights(_myGridGroupData, grinderOwnerIdentityId);
        }

        private void AddBlock(MyCubeBlock block)
        {
            SetOwnership(block);
            BlockTypeController.AddBlock(block);
        }

        private void RegisterEvents()
        {
            WriteGeneral(nameof(RegisterEvents), $"Registering Events for OwnershipType {GridOwnershipController.OwnershipType}");
            if (GridOwnershipController.OwnershipType == OwnershipType.Npc)
                _me.OnFatBlockAdded += OnBlockAdded;
            else _me.OnBlockOwnershipChanged += OnBlockOwnershipChanged;

            _me.OnGridSplit += OnGridSplit;
            _me.OnGridMerge += OnGridMerge;

            if (_initialized) return;
            _me.OnMarkForClose += entity => Close();
        }

        private void DeRegisterEvents()
        {
            _me.OnFatBlockAdded -= OnBlockAdded;
            _me.OnBlockOwnershipChanged -= OnBlockOwnershipChanged;
            _me.OnGridSplit -= OnGridSplit;
            _me.OnGridMerge -= OnGridMerge;
        }

        private void OnBlockAdded(MyCubeBlock block)
        {
            // TODO need to check here for the connector being added from a player ship.  We shouldn't be taking that over.  At the same time, we don't want it connected either. 
            // TODO perhaps add logic that looks for store blocks on the NPC grid and if none found (or the mating connector has trade disabled?) then just unlatch the connectors
            var connector = block as IMyShipConnector;
            if (connector != null && connector.IsFunctional && connector.IsConnected) return; // maybe this works?  
            if (GridOwnershipController.OwnershipType == OwnershipType.Npc)
                _mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => AddBlock(block));
        }

        private void OnBlockOwnershipChanged(MyCubeGrid unused)
        {
            // This only needs to trigger if the grid is not owned by a NPC.  
            // The check only exists to see if we need to take over monitoring the grid or not.
            if (GridOwnershipController.OwnershipType != OwnershipType.Npc)
                _mediator.GridGroupCoordinationController.InitializeOwnership(_myGridGroupData);
        }

        public override void Reset()
        {
            base.Reset();
            IsClosed = true;
            DeRegisterEvents();
            GridOwnershipController.Reset();
            BlockTypeController.Reset();
            _mediator.GridCollectionController.RemoveFromGrids(_me.EntityId);
            BlockTypeController.OnWriteToLog -= WriteGeneral;
            GridOwnershipController.OnWriteToLog -= WriteGeneral;
            GridOwnershipController.SetOwnershipAction -= SetOwnership;
            GridOwnershipController.DisownGridAction -= DisownGrid;
            GridOwnershipController.TakeOverGridAction -= TakeOverGrid;
            GridOwnershipController.IgnoreGridAction -= IgnoreGrid;
        }

        //public override void Close()
        //{
        //    if (IsClosed) return;
            
        //    //base.Close();
        //}

        //public override void WriteGeneral(string caller, string message)
        //{
        //    base.WriteGeneral($"[{_me.EntityId.ToEntityIdFormat()}] {caller}", message);
        //}
    }
}