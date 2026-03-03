using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Infrastructure;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace HostileTakeover2.Thraxus.Models
{
    internal class Construct : BaseLoggingClass
    {
        private MyCubeGrid _me;
        private IMyGridGroupData _myGridGroupData;

        public readonly GridOwnershipController GridOwnershipController = new GridOwnershipController();
        public readonly BlockTypeController BlockTypeController = new BlockTypeController();
        private Mediator _mediator;

        public long CurrentOwnerId => _me.BigOwners.Count != 0 ? _me.BigOwners[0] : 0;
        public long EntityId => _me?.EntityId ?? 0;

        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            SetLogPrefix(grid.EntityId.ToEntityIdFormat());
            WriteGeneral(nameof(Init), $"Primary Initialization for Construct [{grid.EntityId:D18}] starting.");
            IsClosed = false;
            _me = grid;
            _mediator = mediator;
            _me.OnMarkForClose += OnGridMarkedForClose;
            WriteGeneral(nameof(Init), $"Primary Initialization for Construct [{_me.EntityId:D18}] complete.");
            Init();
        }

        private void OnGridMarkedForClose(IMyEntity entity)
        {
            Close();
        }

        private void Init()
        {
            WriteGeneral(nameof(Init), $"Secondary Initialization for Construct [{_me.EntityId:D18}] starting.");
            _mediator.ConstructController.Add(_me.EntityId, this);
            BlockTypeController.OnWriteToLog += WriteGeneral;
            BlockTypeController.Init(_mediator, GridOwnershipController);
            BlockTypeController.OnImportantBlocksEmpty += OnAllImportantBlocksGone;
            GridOwnershipController.OnWriteToLog += WriteGeneral;
            GridOwnershipController.SetOwnershipAction += SetOwnership;
            GridOwnershipController.DisownGridAction += DisownGrid;
            GridOwnershipController.TakeOverGridAction += TakeOverGrid;
            GridOwnershipController.IgnoreGridAction += IgnoreGrid;
            SetupGridGroup();
            WriteGeneral(nameof(Init), $"Secondary Initialization for Construct [{_me.EntityId:D18}] complete.");
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
                if (newGridGroup == null)
                {
                    _mediator.ReturnConstruct(this, _me.EntityId);
                    return;
                }
                SetGridGroupData();
                ReEvaluateOwnership();
                return;
            }
            SetGridGroupData();
        }

        private void OnGridAdded(IMyGridGroupData newGridGroup, IMyCubeGrid newGrid, IMyGridGroupData oldGridGroup)
        {
            WriteGeneral(nameof(OnGridAdded), $"Grid was added.  Adding to IMyGridGroupData for [{(_me.EntityId == newGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{newGrid.EntityId:D18}].");
            BlockTypeController.AddGrid((MyCubeGrid)newGrid);
        }

        private void OnAllImportantBlocksGone()
        {
            foreach (var fatBlock in _me.GetFatBlocks())
                fatBlock.ChangeOwner(0, MyOwnershipShareModeEnum.All);
            DisownGrid();
        }

        public void DisownGrid()
        {
            GridOwnershipController.Reset();
            BlockTypeController.Reset();
            SetOwnership();
            SetEvents();
        }

        private void IgnoreGrid()
        {
            SetEvents();
        }

        private void TakeOverGrid()
        {
            WriteGeneral(nameof(TakeOverGrid), $"Attempting to take over grid: [{(GridOwnershipController.OwnershipType == OwnershipType.Npc).ToSingleChar()}] [{(!BlockTypeController.IsClosed).ToSingleChar()}]");
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
            WriteGeneral(nameof(SetOwnership), $"Grabbing some blocks...");
            BlockTypeController.AddGrid(_me);
            WriteGeneral(nameof(SetOwnership), $"Grabbed some blocks.. {BlockTypeController.GetImportantBlockCount()}");
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
            if (connector != null && connector.IsFunctional && connector.IsConnected) return;
            if (GridOwnershipController.OwnershipType == OwnershipType.Npc)
                _mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => AddBlock(block));
        }

        private void OnBlockOwnershipChanged(MyCubeGrid unused)
        {
            if (GridOwnershipController.OwnershipType != OwnershipType.Npc)
                _mediator.GridGroupCoordinationController.InitializeOwnership(_myGridGroupData);
        }

        public override void Reset()
        {
            base.Reset();
            IsClosed = true;
            _me.OnMarkForClose -= OnGridMarkedForClose;
            DeRegisterEvents();
            GridOwnershipController.Reset();
            BlockTypeController.Reset();
            _mediator.ConstructController.Remove(_me.EntityId);
            BlockTypeController.OnImportantBlocksEmpty -= OnAllImportantBlocksGone;
            BlockTypeController.OnWriteToLog -= WriteGeneral;
            GridOwnershipController.OnWriteToLog -= WriteGeneral;
            GridOwnershipController.SetOwnershipAction -= SetOwnership;
            GridOwnershipController.DisownGridAction -= DisownGrid;
            GridOwnershipController.TakeOverGridAction -= TakeOverGrid;
            GridOwnershipController.IgnoreGridAction -= IgnoreGrid;
        }
    }
}
