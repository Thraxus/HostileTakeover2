using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridGroupOwnershipTypeCoordinationController : BaseLoggingClass
    {
        /// <summary>
        /// The purpose of this controller is to figure out who owns a grid and trigger the OwnerCoordinator to
        /// transfer ownership as needed.  This controller is expected to communicate with the BlockType Controller
        /// during the management of the important blocks collection. 
        /// </summary>

        private readonly HashSet<IMyGridGroupData> _currentGridGroupsUnderReview = new HashSet<IMyGridGroupData>();
        //private readonly Dictionary<OwnerType, int> _reusableOwnershipCounterDictionary = new Dictionary<OwnerType, int>();
        private Mediator _mediator;

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }
        
        //public OwnerType GetGridGroupOwnershipType(IMyGridGroupData myGridGroupData)
        //{
        //    return GridGroupOwnershipEvaluationByImportantBlocks(myGridGroupData);
        //}

        public void SetGridGroupOwnership(IMyGridGroupData myGridGroupData)
        {
            var ownerType = OwnerType.UnderReview;
            if (_currentGridGroupsUnderReview.Contains(myGridGroupData)) return;
            _currentGridGroupsUnderReview.Add(myGridGroupData);
            var gridList = _mediator.GetReusableMyCubeGridList(myGridGroupData);

            long ownerId = 0;
            foreach (var myCubeGrid in gridList)
            {
                GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                bool hasImportantBlocks = grid.BlockTypeController.GridHasImportantBlocks((MyCubeGrid)myCubeGrid);
                ownerId = myCubeGrid.BigOwners[0];
                OwnerType thisGridOwnerType = ownerId == 0 ? OwnerType.None : MyAPIGateway.Players.TryGetSteamId(ownerId) <= 0
                    ? OwnerType.Npc : OwnerType.Player;
                if (!(hasImportantBlocks && thisGridOwnerType == OwnerType.Npc))
                {
                    WriteGeneral(nameof(SetGridGroupOwnership), $"Grid is no longer valid for Npc Ownership: [{hasImportantBlocks.ToSingleChar()}] [{thisGridOwnerType}] [{ownerId.ToEntityIdFormat()}]");
                    ownerType = OwnerType.None;
                    continue;
                }
                WriteGeneral(nameof(SetGridGroupOwnership), $"Grid is valid for Npc Ownership! [{ownerId.ToEntityIdFormat()}]");
                ownerType = thisGridOwnerType;
                break;
            }

            foreach (var myCubeGrid in gridList)
            {
                GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                grid.SetGridOwnership(ownerId, ownerType);
            }

            _mediator.ReturnReusableMyCubeGridList(gridList);
            _currentGridGroupsUnderReview.Remove(myGridGroupData);
        }

        //public OwnerType GridGroupOwnershipEvaluationByImportantBlocks(IMyGridGroupData myGridGroupData)
        //{
        //    OwnerType ownershipType = UpdateGridGroupOwner(myGridGroupData);
        //    if (ownershipType != OwnerType.Npc) return ownershipType;

        //    if (_currentGridGroupsUnderReview.Contains(myGridGroupData)) return OwnerType.UnderReview;
        //    _currentGridGroupsUnderReview.Add(myGridGroupData);
        //    var gridList = _mediator.GetReusableMyCubeGridList(myGridGroupData);
        //    bool releaseNpcOwnership = true;

        //    foreach (var myCubeGrid in gridList)
        //    {
        //        GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
        //        int importantBlocks = grid.BlockTypeController.EnabledImportantBlocks.Count;
        //        WriteGeneral(nameof(GridGroupOwnershipEvaluationByImportantBlocks), $"Evaluating [{grid.EntityId.ToEntityIdFormat()}] | Important Blocks: {importantBlocks} | Ownership Type: {grid.GridOwnership.OwnerType}");
        //        if (grid.GridOwnership.OwnerType == OwnerType.Npc && grid.BaseGrid == null)
        //        {
        //            releaseNpcOwnership = false;
        //            break;
        //        }
        //        if (importantBlocks <= 0 || grid.GridOwnership.OwnerType != OwnerType.Npc) continue;
        //        releaseNpcOwnership = false;
        //        break;
        //    }

        //    _mediator.ReturnReusableMyCubeGridList(gridList);
        //    _currentGridGroupsUnderReview.Remove(myGridGroupData);

        //    if (releaseNpcOwnership) ownershipType = OwnerType.None;
        //    WriteGeneral(nameof(GridGroupOwnershipEvaluationByImportantBlocks), $"Evaluated Npc Ownership eligibility.  Scheduled for release: [{releaseNpcOwnership.ToSingleChar()}]");
        //    return ownershipType;
        //}
        
        //public OwnerType UpdateGridGroupOwner(IMyGridGroupData myGridGroupData)
        //{
        //    if (_currentGridGroupsUnderReview.Contains(myGridGroupData)) return OwnerType.UnderReview;
        //    _currentGridGroupsUnderReview.Add(myGridGroupData);
        //    _reusableOwnershipCounterDictionary.Clear();

        //    var gridList = _mediator.GetReusableMyCubeGridList(myGridGroupData);

        //    foreach (var myCubeGrid in gridList)
        //    {
        //        GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
        //        bool wasUpdated = grid.GridOwnership.UpdateOwner(grid.CurrentOwnerId, myCubeGrid.EntityId);
        //        OwnerType ownershipType = grid.GridOwnership.OwnerType;
        //        AddToOwnershipCounter(ownershipType);
        //        if (ownershipType == OwnerType.Npc)
        //        {
        //            WriteGeneral(nameof(UpdateGridGroupOwner), $"Evaluated Ownership for [{grid.EntityId.ToEntityIdFormat()}].  Npc Found!");
        //            break;
        //        }
        //        WriteGeneral(nameof(UpdateGridGroupOwner), $"Evaluated Ownership for [{grid.EntityId.ToEntityIdFormat()}].  Ownership Type: [{wasUpdated.ToSingleChar()}] {ownershipType}");
        //    }
        //    _mediator.ReturnReusableMyCubeGridList(gridList);
        //    _currentGridGroupsUnderReview.Remove(myGridGroupData);

        //    if (_reusableOwnershipCounterDictionary.ContainsKey(OwnerType.Npc))
        //        return OwnerType.Npc;

        //    return _reusableOwnershipCounterDictionary.ContainsKey(OwnerType.Player) ? OwnerType.Player : OwnerType.None;
        //}

        //private void AddToOwnershipCounter(OwnerType ownershipType)
        //{
        //    if (_reusableOwnershipCounterDictionary.ContainsKey(ownershipType))
        //        _reusableOwnershipCounterDictionary[ownershipType]++;
        //    else _reusableOwnershipCounterDictionary.Add(ownershipType, 1);
        //}
    }
}