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

        //public void SetGridOwnership(IMyGridGroupData myGridGroupData)
        //{
        //    if (_currentGridGroupsUnderReview.Contains(myGridGroupData)) return;
        //    _currentGridGroupsUnderReview.Add(myGridGroupData);

        //    var newOwnerType = _mediator.GridGroupOwnershipTypeCoordinationController.GetGridGroupOwnershipType(myGridGroupData);
            
        //    var gridList = _mediator.GetReusableMyCubeGridList(myGridGroupData);
        //    foreach (var myCubeGrid in gridList)
        //    {
        //        GridController grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
        //        grid.SetOwner(newOwnerType);
        //    }
        //    _mediator.GridGroupOwnerTypeCoordinationController.AddOrUpdateOwnership(myGridGroupData, newOwnerType);
        //    _mediator.ReturnReusableMyCubeGridList(gridList);
            
        //    WriteGeneral(nameof(SetGridOwnership), $"Grid group evaluated and is unchanged.  OwnerType: {newOwnerType}");
        //}
    }
}