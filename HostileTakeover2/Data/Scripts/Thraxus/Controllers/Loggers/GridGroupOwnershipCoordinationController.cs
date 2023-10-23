using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Utility;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridGroupOwnershipCoordinationController : BaseLoggingClass
    {
        private readonly HashSet<IMyGridGroupData> _currentGridGroupsUnderReview = new HashSet<IMyGridGroupData>();
        private Mediator _mediator;

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }

        public void SetGridOwnership(IMyGridGroupData myGridGroupData)
        {
            if (_currentGridGroupsUnderReview.Contains(myGridGroupData)) return;
            _currentGridGroupsUnderReview.Add(myGridGroupData);

            var newOwnerType = _mediator.GridGroupOwnershipTypeCoordinationController.GetGridGroupOwnershipType(myGridGroupData);
            //var currentOwnerType = _mediator.GridGroupOwnerTypeCoordinationController.GetGridGroupOwnershipType(myGridGroupData);

            //WriteGeneral(nameof(SetGridOwnership), $"Grid group evaluation complete.  CurrentOwnerType: [{newOwnerType}]  NewOwnerType: [{newOwnerType}]");

            //if (currentOwnerType != newOwnerType)
            //{
            //    _currentGridGroupsUnderReview.Remove(myGridGroupData);
            //    return;
            //}
            // Owner has not changed, do nothing.
            
            var gridList = _mediator.GetReusableMyCubeGridList(myGridGroupData);
            foreach (var myCubeGrid in gridList)
            {
                GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                grid.SetOwner(newOwnerType);
            }
            _mediator.GridGroupOwnerTypeCoordinationController.AddOrUpdateOwnership(myGridGroupData, newOwnerType);
            _mediator.ReturnReusableMyCubeGridList(gridList);
            
            WriteGeneral(nameof(SetGridOwnership), $"Grid group evaluated and is unchanged.  OwnerType: {newOwnerType}");
            
            //switch (currentOwnerType)
            //{
            //    case OwnerType.None:
            //    case OwnerType.NotEvaluated:
            //    case OwnerType.UnderReview:
            //    default:
            //        switch (newOwnerType)
            //        {
            //            case OwnerType.None: // All three
            //            case OwnerType.NotEvaluated:  // Of these ownership types
            //            case OwnerType.UnderReview: // Are stupid
            //            default:
            //                break;
            //            case OwnerType.Npc: // Wake the fuck up asshole, you've got work to do!
            //                // Trigger new Npc ownership
            //                break;
            //            case OwnerType.Player:  // Go to sleep sunshine, boring player is boring.
            //                if (currentOwnerType == OwnerType.None)
            //                    // Do nothing
            //                    break;
            //                TriggerGridOwnershipChange(newOwnerType, )
            //                // Trigger new Player ownership (same as None ownership really)
            //                break;

            //        }
            //        break;
            //    case OwnerType.Npc:
            //        switch (newOwnerType)
            //        {
            //            case OwnerType.NotEvaluated:  // was Npc now NotEvaluated is... odd...
            //            case OwnerType.Npc:  // Was Npc now Npc can't happen here
            //            case OwnerType.UnderReview:  // was Npc now UnderReview is... odd...
            //            default:
            //                // Do Nothing
            //                break;
            //            case OwnerType.None:  // Those asshole players finally got the best of me...  Alas, poor Yorick...
            //                // Trigger grid disown and put in holding pattern
            //                break;
            //            case OwnerType.Player: // Fucking nuclear admin actions.  How rude!
            //                // Trigger ignore
            //                break;
            //        }
            //        break;
            //    case OwnerType.Player:
            //        switch (newOwnerType)
            //        {
            //            case OwnerType.None:  // Do nothing, both player owned and none owned should be in a holding pattern
            //            case OwnerType.NotEvaluated:  // Grid is still new, not sure how it got here like this though
            //            case OwnerType.Player:  // Was player now player actually happen here
            //            case OwnerType.UnderReview:  // Grid is being processed, so just wait for that to finish
            //            default:
            //                break;
            //            case OwnerType.Npc:  // Was player now Npc means they transferred ownership, so kick the shit in fuck you mode
            //                break;
            //        }
            //        break;
        }
    }
}