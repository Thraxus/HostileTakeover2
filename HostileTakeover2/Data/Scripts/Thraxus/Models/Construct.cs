using System.Collections.Generic;
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
        private Mediator _mediator;

        public readonly GridOwnershipController GridOwnershipController = new GridOwnershipController();
        public readonly BlockController BlockController = new BlockController();
        public readonly GridGroupManager GridGroupManager = new GridGroupManager();
        private readonly Dictionary<long, int> _ownershipTally = new Dictionary<long, int>();

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
            BlockController.OnWriteToLog += WriteGeneral;
            BlockController.Init(_mediator, GridOwnershipController);
            BlockController.OnImportantBlocksEmpty += OnAllImportantBlocksGone;
            GridOwnershipController.OnWriteToLog += WriteGeneral;
            GridOwnershipController.SetOwnershipAction += SetOwnership;
            GridOwnershipController.DisownGridAction += DisownGrid;
            GridOwnershipController.TakeOverGridAction += TakeOverGrid;
            GridOwnershipController.IgnoreGridAction += IgnoreGrid;
            GridGroupManager.OnWriteToLog += WriteGeneral;
            GridGroupManager.GridAddedAction += OnGridAdded;
            GridGroupManager.GridRemovedAction += OnGridRemoved;
            SetupGridGroup();
            WriteGeneral(nameof(Init), $"Secondary Initialization for Construct [{_me.EntityId:D18}] complete.");
        }

        private void SetupGridGroup()
        {
            if (IsClosed) return;
            GridGroupManager.Init(_me);
            EvaluateOwnership();
        }

        private void ReEvaluateOwnership()
        {
            if (GridGroupManager.GridGroupData == null) return;
            long ownerId = CalculateGroupOwnerId(GridGroupManager.GridGroupData);
            WriteGeneral(nameof(ReEvaluateOwnership), $"Reevaluating ownership.  Current Rightful Owner: [{GridOwnershipController.RightfulOwner.ToEntityIdFormat()}]");
            if (GridOwnershipController.RightfulOwner == ownerId) return;
            SetGroupOwnership(GridGroupManager.GridGroupData, ownerId);
        }

        private void EvaluateOwnership()
        {
            if (GridGroupManager.GridGroupData == null) return;
            long ownerId = CalculateGroupOwnerId(GridGroupManager.GridGroupData);
            WriteGeneral(nameof(EvaluateOwnership), $"Grid group owner determined to be {ownerId:D18}");
            SetGroupOwnership(GridGroupManager.GridGroupData, ownerId);
        }

        private long CalculateGroupOwnerId(IMyGridGroupData groupData)
        {
            _ownershipTally.Clear();
            var gridList = _mediator.GetReusableCubeGridList(groupData);
            foreach (var grid in gridList)
            {
                Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                if (construct == null) continue;
                long id = construct.CurrentOwnerId;
                if (_ownershipTally.ContainsKey(id))
                    _ownershipTally[id]++;
                else
                    _ownershipTally.Add(id, 1);
            }
            _mediator.ReturnReusableCubeGridList(gridList);
            long ownerId = 0;
            int count = 0;
            foreach (var kvp in _ownershipTally)
            {
                if (kvp.Value <= count) continue;
                ownerId = kvp.Key;
                count = kvp.Value;
            }
            return ownerId;
        }

        private void SetGroupOwnership(IMyGridGroupData groupData, long ownerId)
        {
            var gridList = _mediator.GetReusableCubeGridList(groupData);
            foreach (var grid in gridList)
            {
                Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                if (construct == null) continue;
                construct.GridOwnershipController.SetOwnership(ownerId);
            }
            _mediator.ReturnReusableCubeGridList(gridList);
        }

        private void OnGridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        {
            WriteGeneral(nameof(OnGridSplit), $"Grid Split -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
            BlockController.RemoveOldBlocks(newGrid);
            ReEvaluateOwnership();
        }

        private void OnGridMerge(MyCubeGrid newGrid, MyCubeGrid oldGrid)
        {
            WriteGeneral(nameof(OnGridMerge), $"Grid Merge -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
            BlockController.AddGrid(oldGrid);
            GridGroupManager.Refresh();
        }

        private void OnGridRemoved(IMyCubeGrid removedGrid, IMyGridGroupData newGridGroup)
        {
            WriteGeneral(nameof(OnGridRemoved), $"Grid was removed.  Resetting IMyGridGroupData for [{(_me.EntityId == removedGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{removedGrid.EntityId:D18}].");
            if (removedGrid == _me)
            {
                if (newGridGroup == null)
                {
                    _mediator.ReturnConstruct(this, _me.EntityId);
                    return;
                }
                GridGroupManager.Refresh();
                ReEvaluateOwnership();
                return;
            }
            GridGroupManager.Refresh();
        }

        private void OnGridAdded(IMyCubeGrid newGrid)
        {
            WriteGeneral(nameof(OnGridAdded), $"Grid was added.  Adding to IMyGridGroupData for [{(_me.EntityId == newGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{newGrid.EntityId:D18}].");
        }

        private void OnAllImportantBlocksGone()
        {
            var groupData = GridGroupManager.GridGroupData;
            if (groupData == null)
            {
                foreach (var fatBlock in _me.GetFatBlocks())
                    fatBlock.ChangeOwner(0, MyOwnershipShareModeEnum.All);
                DisownGrid();
                return;
            }

            var gridList = _mediator.GetReusableCubeGridList(groupData);
            bool anyHasBlocks = false;
            foreach (var grid in gridList)
            {
                Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                if (construct != null && construct.BlockController.GetImportantBlockCount() > 0)
                {
                    anyHasBlocks = true;
                    break;
                }
            }

            if (!anyHasBlocks)
            {
                foreach (var grid in gridList)
                {
                    foreach (var fatBlock in ((MyCubeGrid)grid).GetFatBlocks())
                        fatBlock.ChangeOwner(0, MyOwnershipShareModeEnum.All);
                    Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                    construct?.DisownGrid();
                }
            }
            _mediator.ReturnReusableCubeGridList(gridList);
        }

        public void DisownGrid()
        {
            GridOwnershipController.Reset();
            BlockController.Reset();
            SetOwnership();
            SetEvents();
        }

        private void IgnoreGrid()
        {
            SetEvents();
        }

        private void TakeOverGrid()
        {
            WriteGeneral(nameof(TakeOverGrid), $"Attempting to take over grid: [{(GridOwnershipController.OwnershipType == OwnershipType.Npc).ToSingleChar()}] [{(!BlockController.IsClosed).ToSingleChar()}]");
            if (GridOwnershipController.OwnershipType != OwnershipType.Npc || BlockController.IsClosed) return;
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
            BlockController.AddGrid(_me);
            WriteGeneral(nameof(SetOwnership), $"Grabbed some blocks.. {BlockController.GetImportantBlockCount()}");
        }

        private void SetOwnership(MyCubeBlock block)
        {
            block.ChangeOwner(block.IsFunctional ? GridOwnershipController.RightfulOwner : 0, MyOwnershipShareModeEnum.None);
        }

        public void TriggerHighlights(long grinderOwnerIdentityId)
        {
            if (!_mediator.DefaultSettings.UseHighlights.Current) return;
            if (GridGroupManager.GridGroupData == null) return;
            _mediator.HighlightController.EnableHighlights(GridGroupManager.GridGroupData, grinderOwnerIdentityId);
        }

        private void AddBlock(MyCubeBlock block)
        {
            SetOwnership(block);
            BlockController.AddBlock(block);
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
            if (GridOwnershipController.OwnershipType != OwnershipType.Npc && GridGroupManager.GridGroupData != null)
                EvaluateOwnership();
        }

        public override void Reset()
        {
            base.Reset();
            IsClosed = true;
            _me.OnMarkForClose -= OnGridMarkedForClose;
            DeRegisterEvents();
            GridOwnershipController.Reset();
            BlockController.Reset();
            _mediator.ConstructController.Remove(_me.EntityId);
            BlockController.OnImportantBlocksEmpty -= OnAllImportantBlocksGone;
            BlockController.OnWriteToLog -= WriteGeneral;
            GridOwnershipController.OnWriteToLog -= WriteGeneral;
            GridOwnershipController.SetOwnershipAction -= SetOwnership;
            GridOwnershipController.DisownGridAction -= DisownGrid;
            GridOwnershipController.TakeOverGridAction -= TakeOverGrid;
            GridOwnershipController.IgnoreGridAction -= IgnoreGrid;
            GridGroupManager.GridAddedAction -= OnGridAdded;
            GridGroupManager.GridRemovedAction -= OnGridRemoved;
            GridGroupManager.OnWriteToLog -= WriteGeneral;
            GridGroupManager.Reset();
        }
    }
}
