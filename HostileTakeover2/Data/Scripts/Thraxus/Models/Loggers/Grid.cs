using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Controllers;
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
        public long EntityId => _me.EntityId;

        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            WriteGeneral(nameof(Init), $"Grid Initializing for [{grid.EntityId:D18}]!");
            IsClosed = false;
            _initialized = true;
            _me = grid;
            _mediator = mediator;
            WriteGeneral(nameof(Init), $"Grid Online for [{_me.EntityId:D18}]!");
            Init();
        }

        private void Init()
        {
            WriteGeneral(nameof(Init), $"Secondary Initialization firing for [{_me.EntityId:D18}]!");
            _mediator.GridCollectionController.AddToGrids(_me.EntityId, this);
            BlockTypeController.OnWriteToLog += WriteGeneral;
            BlockTypeController.Init(_mediator, GridOwnershipController);
            GridOwnershipController.OnWriteToLog += WriteGeneral;
            GridOwnershipController.SetOwnershipAction += SetOwnership;
            GridOwnershipController.DisownGridAction += DisownGrid;
            GridOwnershipController.TakeOverGridAction += TakeOverGrid;
            GridOwnershipController.IgnoreGridAction += IgnoreGrid;
            SetupGridGroup();
            RegisterEvents();
            _mediator.GridGroupCoordinationController.CoordinateOwnership(_myGridGroupData);
            WriteGeneral(nameof(Init), $"Secondary Initialization complete for [{_me.EntityId:D18}]!");
        }

        private void SetupGridGroup()
        {
            if (IsClosed) return;
            _myGridGroupData = _me.GetGridGroup(GridLinkTypeEnum.Logical);
            _myGridGroupData.OnGridRemoved += OnGridRemoved;
            _myGridGroupData.OnReleased += delegate
            {
                _myGridGroupData.OnGridRemoved -= OnGridRemoved;
            };
        }

        private void OnGridRemoved(IMyGridGroupData thisGridGroup, IMyCubeGrid removedGrid, IMyGridGroupData newGridGroup)
        {
            if (removedGrid.EntityId == _me.EntityId)
            {
                _myGridGroupData.OnGridRemoved -= OnGridRemoved;
                SetupGridGroup();
            }
            _mediator.GridGroupCoordinationController.CoordinateOwnership(_myGridGroupData);
        }

        public void DisownGrid()
        {
            GridOwnershipController.Reset();
            BlockTypeController.Reset();
            SetOwnership();
        }

        private void IgnoreGrid()
        {
            DeRegisterEvents();
            RegisterEvents();
        }

        private void TakeOverGrid()
        {
            WriteGeneral(nameof(TakeOverGrid), $"Attempting to take over grid: [{(!_mediator.DefaultSettings.CapturePlayerBlocks.Current).ToSingleChar()}] [{(GridOwnershipController.OwnershipType == OwnershipType.Npc).ToSingleChar()}] [{(!BlockTypeController.IsClosed).ToSingleChar()}]");
            if (!_mediator.DefaultSettings.CapturePlayerBlocks.Current) return;
            if (GridOwnershipController.OwnershipType == OwnershipType.Npc && !BlockTypeController.IsClosed)
                SetOwnership();
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
            if (GridOwnershipController.OwnershipType == OwnershipType.Npc)
                _me.OnFatBlockAdded += OnBlockAdded;
            else _me.OnBlockOwnershipChanged += OnBlockOwnershipChanged;

            if (_initialized) return;
            _me.OnMarkForClose += entity => Close();
        }
        
        private void DeRegisterEvents()
        {
            _me.OnFatBlockAdded -= OnBlockAdded;
            _me.OnBlockOwnershipChanged -= OnBlockOwnershipChanged;
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
                _mediator.GridGroupCoordinationController.CoordinateOwnership(_myGridGroupData);
        }

        public override void Reset()
        {
            base.Reset();
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

        public override void Close()
        {
            if (IsClosed) return;
            _mediator.ReturnGrid(this);
            base.Close();
        }
    }
}