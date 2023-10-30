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
    public class GridGroupOwnershipTypeCoordinationController : BaseLoggingClass
    {
        /// <summary>
        /// The purpose of this controller is to figure out who owns a grid and trigger the OwnerCoordinator to
        /// transfer ownership as needed.  This controller is expected to communicate with the BlockType Controller
        /// during the management of the important blocks collection. 
        /// </summary>

        private readonly HashSet<IMyGridGroupData> _currentGridGroupsUnderReview = new HashSet<IMyGridGroupData>();
        private readonly Mediator _mediator;

        public GridGroupOwnershipTypeCoordinationController(Mediator mediator)
        {
            _mediator = mediator;
        }

        public void SetGridGroupOwnership(IMyGridGroupData myGridGroupData)
        {
            WriteGeneral(nameof(SetGridGroupOwnership), $"Attempting to set grid ownership...");
            var ownerType = OwnerType.UnderReview;
            if (_currentGridGroupsUnderReview.Contains(myGridGroupData)) return;
            _currentGridGroupsUnderReview.Add(myGridGroupData);
            
            var gridList = _mediator.GetGridGroupCollection(myGridGroupData);

            long ownerId = 0;
            foreach (var myCubeGrid in gridList)
            {
                GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                bool hasImportantBlocks = grid.BlockTypeController.GridHasImportantBlocks((MyCubeGrid)myCubeGrid);
                ownerId = myCubeGrid.BigOwners[0];
                OwnerType thisGridOwnerType = ownerId == 0 ? OwnerType.None : MyAPIGateway.Players.TryGetSteamId(ownerId) <= 0
                    ? OwnerType.Npc : OwnerType.Player;
                
                if (thisGridOwnerType == OwnerType.Player)
                {
                    WriteGeneral(nameof(SetGridGroupOwnership), $"GridGroup is Player Owned: [{thisGridOwnerType}] [{hasImportantBlocks.ToSingleChar()}] [{ownerId.ToEntityIdFormat()}]");
                    break;
                }

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

            WriteGeneral(nameof(SetGridGroupOwnership), $"GridGroup final stats: [{ownerType}] [{ownerId.ToEntityIdFormat()}]");
            foreach (var myCubeGrid in gridList)
            {
                GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                grid.SetGridOwnership(ownerId, ownerType);
            }

            _currentGridGroupsUnderReview.Remove(myGridGroupData);
        }

        public override void Close()
        {
            base.Close();
            _currentGridGroupsUnderReview.Clear();
        }
    }
}