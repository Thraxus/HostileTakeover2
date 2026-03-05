using System;
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
        private bool _ownershipChangePending;

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
            try { Close(); }
            catch (Exception e) { WriteGeneral(nameof(OnGridMarkedForClose), $"Exception: {e}"); }
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
            GridGroupManager.GridRemovedAction += OnGridRemoved;
            SetupGridGroup();
            WriteGeneral(nameof(Init), $"Secondary Initialization for Construct [{_me.EntityId:D18}] complete.");
        }

        private void SetupGridGroup()
        {
            if (IsClosed) return;
            GridGroupManager.Init(_me);
            EvaluateOwnership();
            SetEvents();
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
            if (GridOwnershipController.RightfulOwner == ownerId) return;
            SetGroupOwnership(GridGroupManager.GridGroupData, ownerId);
        }

        private long CalculateGroupOwnerId(IMyGridGroupData groupData)
        {
            _ownershipTally.Clear();
            var gridList = _mediator.GetReusableCubeGridList(groupData);
            foreach (var grid in gridList)
            {
                foreach (var fatBlock in ((MyCubeGrid)grid).GetFatBlocks())
                {
                    long id = fatBlock.OwnerId;
                    if (id == 0 || MyAPIGateway.Players.TryGetSteamId(id) > 0) continue;
                    if (_ownershipTally.ContainsKey(id))
                        _ownershipTally[id]++;
                    else
                        _ownershipTally.Add(id, 1);
                }
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
                var cubeGrid = (MyCubeGrid)grid;
                bool hasOwnerBlocks = false;
                foreach (var fatBlock in cubeGrid.GetFatBlocks())
                    if (fatBlock.OwnerId == ownerId) { hasOwnerBlocks = true; break; }
                if (!hasOwnerBlocks) continue;
                Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                if (construct == null) continue;
                construct.GridOwnershipController.SetOwnership(ownerId);
            }
            _mediator.ReturnReusableCubeGridList(gridList);
        }

        private void OnGridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        {
            try
            {
                WriteGeneral(nameof(OnGridSplit), $"Grid Split -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
                BlockController.RemoveOldBlocks(newGrid);
                ReEvaluateOwnership();
            }
            catch (Exception e) { WriteGeneral(nameof(OnGridSplit), $"Exception: {e}"); }
        }

        private void OnGridMerge(MyCubeGrid newGrid, MyCubeGrid oldGrid)
        {
            try
            {
                WriteGeneral(nameof(OnGridMerge), $"Grid Merge -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
                BlockController.AddGrid(oldGrid);
                GridGroupManager.Refresh();
            }
            catch (Exception e) { WriteGeneral(nameof(OnGridMerge), $"Exception: {e}"); }
        }

        private void OnGridRemoved(IMyCubeGrid removedGrid, IMyGridGroupData newGridGroup)
        {
            try
            {
                WriteGeneral(nameof(OnGridRemoved), $"Grid was removed.  Resetting IMyGridGroupData for [{(_me.EntityId == removedGrid.EntityId).ToSingleChar()}] [{_me.EntityId:D18}] [{removedGrid.EntityId:D18}].");
                if (removedGrid == _me)
                {
                    WriteGeneral(nameof(OnGridRemoved), $"This construct's grid was the removed grid. NewGridGroup null: [{(newGridGroup == null).ToSingleChar()}]");
                    if (newGridGroup == null)
                    {
                        WriteGeneral(nameof(OnGridRemoved), $"No new grid group — returning construct to pool: [{_me.EntityId:D18}]");
                        _mediator.ReturnConstruct(this);
                        return;
                    }
                    GridGroupManager.Refresh(newGridGroup);
                    if (BlockController.GetImportantBlockCount() == 0 && !BlockController.IsClosed)
                        OnAllImportantBlocksGone();
                    else
                        ReEvaluateOwnership();
                    return;
                }
                GridGroupManager.Refresh();
                if (BlockController.GetImportantBlockCount() == 0 && !BlockController.IsClosed)
                    OnAllImportantBlocksGone();
            }
            catch (Exception e) { WriteGeneral(nameof(OnGridRemoved), $"Exception: {e}"); }
        }

        private void OnAllImportantBlocksGone()
        {
            try
            {
                var groupData = GridGroupManager.GridGroupData;
                if (groupData == null)
                {
                    _me.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
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
                        var cubeGrid = (MyCubeGrid)grid;
                        if (cubeGrid.BigOwners.Count > 0 && MyAPIGateway.Players.TryGetSteamId(cubeGrid.BigOwners[0]) > 0)
                            continue;
                        cubeGrid.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                        Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                        construct?.DisownGrid();
                    }
                }
                _mediator.ReturnReusableCubeGridList(gridList);
            }
            catch (Exception e) { WriteGeneral(nameof(OnAllImportantBlocksGone), $"Exception: {e}"); }
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
            GridOwnershipController.SetOwnershipAction -= SetOwnership;
            GridOwnershipController.SetOwnershipAction += SetOwnership;
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
            if (_mediator.DefaultSettings.AllowPlayerHacking.Current) return;
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
            {
                _me.OnFatBlockAdded += OnBlockAdded;
                if (!_mediator.DefaultSettings.AllowPlayerHacking.Current)
                    _me.OnBlockOwnershipChanged += ReclaimHackedBlocks;
            }
            else _me.OnBlockOwnershipChanged += OnBlockOwnershipChanged;

            _me.OnGridSplit += OnGridSplit;
            _me.OnGridMerge += OnGridMerge;
        }

        private void DeRegisterEvents()
        {
            _me.OnFatBlockAdded -= OnBlockAdded;
            _me.OnBlockOwnershipChanged -= OnBlockOwnershipChanged;
            _me.OnBlockOwnershipChanged -= ReclaimHackedBlocks;
            _me.OnGridSplit -= OnGridSplit;
            _me.OnGridMerge -= OnGridMerge;
        }

        private void OnBlockAdded(MyCubeBlock block)
        {
            try
            {
                var connector = block as IMyShipConnector;
                if (connector != null && connector.IsFunctional && connector.IsConnected) return;
                if (GridOwnershipController.OwnershipType == OwnershipType.Npc)
                    _mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => AddBlock(block));
            }
            catch (Exception e) { WriteGeneral(nameof(OnBlockAdded), $"Exception: {e}"); }
        }

        private void ReclaimHackedBlocks(MyCubeGrid grid)
        {
            try
            {
                foreach (var fatBlock in _me.GetFatBlocks())
                {
                    long expected = fatBlock.IsFunctional ? GridOwnershipController.RightfulOwner : 0;
                    if (fatBlock.OwnerId != expected)
                        fatBlock.ChangeOwner(expected, MyOwnershipShareModeEnum.None);
                }
            }
            catch (Exception e) { WriteGeneral(nameof(ReclaimHackedBlocks), $"Exception: {e}"); }
        }

        private void OnBlockOwnershipChanged(MyCubeGrid unused)
        {
            try
            {
                if (GridOwnershipController.OwnershipType == OwnershipType.Npc || GridGroupManager.GridGroupData == null) return;
                if (_ownershipChangePending) return;
                _ownershipChangePending = true;
                _mediator.ActionQueue.Add(DefaultSettings.OwnershipChangeDebounceDelay, () =>
                {
                    _ownershipChangePending = false;
                    EvaluateOwnership();
                });
            }
            catch (Exception e) { WriteGeneral(nameof(OnBlockOwnershipChanged), $"Exception: {e}"); }
        }

        public override void Reset()
        {
            base.Reset();
            IsClosed = true;
            _ownershipChangePending = false;
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
            GridGroupManager.GridRemovedAction -= OnGridRemoved;
            GridGroupManager.OnWriteToLog -= WriteGeneral;
            GridGroupManager.Reset();
        }
    }
}
