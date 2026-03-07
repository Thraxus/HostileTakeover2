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
using Sandbox.ModAPI.Weapons;
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
        private readonly HashSet<IMyCubeGrid> _groupGrids = new HashSet<IMyCubeGrid>();
        private bool _ownershipChangePending;

        public long CurrentOwnerId => _me.BigOwners.Count != 0 ? _me.BigOwners[0] : 0;
        public long EntityId => _me?.EntityId ?? 0;

        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            SetLogPrefix(grid.EntityId.ToEntityIdFormat());
            if (mediator.DefaultSettings.IsVerboseActiveFor(DebugType.Construct))
                WriteGeneral(DebugType.Construct, nameof(Init), $"Primary Initialization for Construct [{grid.EntityId:D18}] starting.");
            IsClosed = false;
            _me = grid;
            _mediator = mediator;
            _me.OnMarkForClose += OnGridMarkedForClose;
            if (mediator.DefaultSettings.IsVerboseActiveFor(DebugType.Construct))
                WriteGeneral(DebugType.Construct, nameof(Init), $"Primary Initialization for Construct [{_me.EntityId:D18}] complete.");
            WireUp();
        }

        private void OnGridMarkedForClose(IMyEntity entity)
        {
            try { Close(); }
            catch (Exception e) { WriteGeneral(nameof(OnGridMarkedForClose), $"Exception: {e}"); }
        }

        private void WireUp()
        {
            if (_mediator.DefaultSettings.IsVerboseActiveFor(DebugType.Construct))
                WriteGeneral(DebugType.Construct, nameof(WireUp), $"Wiring Construct [{_me.EntityId:D18}].");
            _mediator.ConstructController.Add(_me.EntityId, this);
            BlockController.OnWriteToLog += WriteGeneral;
            BlockController.Init(_mediator, GridOwnershipController);
            BlockController.OnImportantBlocksEmpty += OnAllImportantBlocksGone;
            GridOwnershipController.OnWriteToLog += WriteGeneral;
            GridOwnershipController.Init(_me, BlockController, _mediator.IsNpcIdentity);
            GridGroupManager.OnWriteToLog += WriteGeneral;
            GridGroupManager.GridRemovedAction += OnGridRemoved;
            GridGroupManager.Init(_me);
        }

        public void Evaluate()
        {
            if (IsClosed) return;
            EvaluateOwnership();
            SetEvents();
        }

        private void ReEvaluateOwnership()
        {
            if (GridGroupManager.GridGroupData == null) return;
            long ownerId = CalculateGroupOwnerId(GridGroupManager.GridGroupData);
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Ownership))
                WriteGeneral(DebugType.Ownership, nameof(ReEvaluateOwnership), $"Reevaluating ownership.  Current Rightful Owner: [{GridOwnershipController.RightfulOwner.ToEntityIdFormat()}]");
            if (GridOwnershipController.RightfulOwner == ownerId) return;
            SetGroupOwnership(ownerId);
        }

        private void EvaluateOwnership()
        {
            if (GridGroupManager.GridGroupData == null) return;
            long ownerId = CalculateGroupOwnerId(GridGroupManager.GridGroupData);
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Ownership))
                WriteGeneral(DebugType.Ownership, nameof(EvaluateOwnership), $"Grid group owner determined to be {ownerId:D18}");
            if (GridOwnershipController.RightfulOwner == ownerId) return;
            SetGroupOwnership(ownerId);
        }

        private long CalculateGroupOwnerId(IMyGridGroupData groupData)
        {
            _ownershipTally.Clear();
            _groupGrids.Clear();
            groupData.GetGrids(_groupGrids);
            foreach (var grid in _groupGrids)
            {
                foreach (var fatBlock in ((MyCubeGrid)grid).GetFatBlocks())
                {
                    long id = fatBlock.OwnerId;
                    // id == 0: unowned or ownership not yet applied at load time (SE doesn't fire
                    // ownership events during world load, so blocks can appear with no owner briefly).
                    // !IsNpcIdentity: player-owned blocks don't vote — only NPC blocks determine NPC ownership.
                    if (id == 0 || !_mediator.IsNpcIdentity(id)) continue;
                    if (_ownershipTally.ContainsKey(id))
                        _ownershipTally[id]++;
                    else
                        _ownershipTally.Add(id, 1);
                }
            }
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

        private void SetGroupOwnership(long ownerId)
        {
            foreach (var grid in _groupGrids)
            {
                var cubeGrid = (MyCubeGrid)grid;
                bool hasOwnerBlocks = false;
                foreach (var fatBlock in cubeGrid.GetFatBlocks())
                    if (fatBlock.OwnerId == ownerId) { hasOwnerBlocks = true; break; }
                // Don't claim grids that don't already have at least one block owned by the NPC —
                // connected player grids live in the same logical group and we don't want to touch them.
                if (!hasOwnerBlocks) continue;
                Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                if (construct == null) continue;
                // Skip constructs already set to this owner — avoids redundant re-init on every
                // group setup call, which would turn group evaluation into an O(n²) mess.
                if (construct.GridOwnershipController.RightfulOwner == ownerId) continue;
                construct.ApplyOwnership(ownerId);
            }
        }

        private void OnGridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        {
            try
            {
                WriteGeneral(nameof(OnGridSplit), $"Grid Split -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
                BlockController.RemoveOldBlocks(newGrid);
                if (BlockController.GetImportantBlockCount() == 0 && !BlockController.IsClosed)
                    OnAllImportantBlocksGone();
                else
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
            // When the last important block is ground down on any construct in the group, every
            // construct in that group fires this event. Only the NPC-owned ones should act on it —
            // the early return prevents the player-owned constructs in the same logical group from
            // running the full disown pass unnecessarily.
            if (GridOwnershipController.OwnershipType != OwnershipType.Npc) return;
            try
            {
                var groupData = GridGroupManager.GridGroupData;
                if (groupData == null)
                {
                    DisownGrid();
                    return;
                }

                _groupGrids.Clear();
                groupData.GetGrids(_groupGrids);
                bool anyHasBlocks = false;
                bool anyPending   = false;

                foreach (var grid in _groupGrids)
                {
                    Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                    if (construct == null)
                    {
                        // Null construct = either a player grid (expected) or an NPC grid not yet
                        // initialized. If it's NPC-owned, defer the decision until it's ready.
                        var cubeGrid = (MyCubeGrid)grid;
                        long owner = cubeGrid.BigOwners.Count > 0 ? cubeGrid.BigOwners[0] : 0;
                        if (owner != 0 && _mediator.IsNpcIdentity(owner))
                            anyPending = true;
                        continue;
                    }
                    if (construct.BlockController.GetImportantBlockCount() > 0)
                    {
                        anyHasBlocks = true;
                        break;
                    }
                    if (construct.BlockController.HasPendingAdds)
                        anyPending = true;
                }

                // anyHasBlocks: another construct in the group still has important blocks — not done yet.
                // anyPending: an NPC construct hasn't finished its deferred block-add pass, so we can't
                // conclude it has no important blocks. Retry after a short delay instead of disowning early.
                if (anyHasBlocks || anyPending)
                {
                    if (anyPending)
                        _mediator.ActionQueue.Add(DefaultSettings.MinorTickDelay + 11, OnAllImportantBlocksGone);
                    return;
                }

                // All constructs accounted for, none pending, none have important blocks — disown.
                DisownConstruct();
            }
            catch (Exception e) { WriteGeneral(nameof(OnAllImportantBlocksGone), $"Exception: {e}"); }
        }

        public void DisownGrid()
        {
            GridOwnershipController.DisownGrid();
            SetEvents();
        }

        public void ApplyOwnership(long ownerId)
        {
            GridOwnershipController.SetOwnership(ownerId);
            SetEvents();
        }

        private void DisownConstruct()
        {
            foreach (var grid in _groupGrids)
            {
                var cubeGrid = (MyCubeGrid)grid;
                if (cubeGrid.BigOwners.Count > 0 && !_mediator.IsNpcIdentity(cubeGrid.BigOwners[0]))
                    continue;
                Construct construct = _mediator.ConstructController.GetConstruct(grid.EntityId);
                construct?.DisownGrid();
            }
        }

        private void SetEvents()
        {
            DeRegisterEvents();
            RegisterEvents();
        }

        public void TriggerHighlights(IMyAngleGrinder grinder)
        {
            if (!_mediator.DefaultSettings.UseHighlights.Current) return;
            if (GridGroupManager.GridGroupData == null) return;
            _mediator.HighlightController.EnableHighlights(GridGroupManager.GridGroupData, grinder);
        }

        private void AddBlock(MyCubeBlock block)
        {
            if (!_mediator.DefaultSettings.AllowPlayerHacking.Current)
                GridOwnershipController.SetBlockOwnership(block);
            BlockController.AddBlock(block);
        }

        private void RegisterEvents()
        {
            if (_mediator.DefaultSettings.IsVerboseActiveFor(DebugType.Construct))
                WriteGeneral(DebugType.Construct, nameof(RegisterEvents), $"Registering Events for OwnershipType {GridOwnershipController.OwnershipType}");
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
                // When two grids connect via a connector, SE fires OnFatBlockAdded for every block
                // on the joining grid. We don't want to re-classify blocks that belong to the other
                // grid — skip if the block is a live, connected connector (the thing that just docked).
                var connector = block as IMyShipConnector;
                if (connector != null && connector.IsFunctional && connector.IsConnected) return;
                if (GridOwnershipController.OwnershipType == OwnershipType.Npc)
                    _mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => AddBlock(block));
            }
            catch (Exception e) { WriteGeneral(nameof(OnBlockAdded), $"Exception: {e}"); }
        }

        private void ReclaimHackedBlocks(MyCubeGrid grid)
        {
            GridOwnershipController.ReclaimHackedBlocks();
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
            // IsClosed = true BEFORE deregistering events and resetting sub-controllers.
            // This prevents any in-flight deferred actions (e.g. deferred AddBlock callbacks)
            // from re-triggering OnImportantBlocksEmpty after we've already started tearing down.
            IsClosed = true;
            _ownershipChangePending = false;
            _groupGrids.Clear();
            _me.OnMarkForClose -= OnGridMarkedForClose;
            DeRegisterEvents();
            GridOwnershipController.Reset();
            BlockController.Reset();
            _mediator.ConstructController.Remove(_me.EntityId);
            BlockController.OnImportantBlocksEmpty -= OnAllImportantBlocksGone;
            BlockController.OnWriteToLog -= WriteGeneral;
            GridOwnershipController.OnWriteToLog -= WriteGeneral;
            GridGroupManager.GridRemovedAction -= OnGridRemoved;
            GridGroupManager.OnWriteToLog -= WriteGeneral;
            GridGroupManager.Reset();
        }
    }
}
