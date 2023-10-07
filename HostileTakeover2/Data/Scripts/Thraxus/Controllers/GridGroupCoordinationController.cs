using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GridGroupCoordinationController : BaseLoggingClass
    {
        private Mediator _mediator;
        //private readonly List<IMyCubeGrid> _reusableGridCollection = new List<IMyCubeGrid>();
        //private readonly List<IMyCubeGrid> _reusableGridCollection = new List<IMyCubeGrid>();
        private readonly Dictionary<long, int> _reusableOwnershipDictionary = new Dictionary<long, int>();

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }
        
        public void CoordinateOwnership(IMyGridGroupData myGridGroupData)
        {
            IterateGridGroup(myGridGroupData);
        }

        private void IterateGridGroup(IMyGridGroupData myGridGroupData)
        {
            //List<IMyCubeGrid> reusableGridCollection = new List<IMyCubeGrid>();
            //_reusableGridCollection.Clear();
            _reusableOwnershipDictionary.Clear();
            //myGridGroupData.GetGrids(reusableGridCollection);
            var gridList = _mediator.GridGroupCollectionController.Get(myGridGroupData);

            foreach (var myCubeGrid in gridList)
            {
                Grid grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                AddToOwnershipDictionary(grid.CurrentOwnerId);
            }

            long ownerId = CalculateOwnerFromOwnershipDictionary();
            SetGridGroupOwnership(ownerId, gridList);
            WriteGeneral(nameof(IterateGridGroup), $"Grid group owner determined to be {ownerId:D18}");
        }

        private void AddToOwnershipDictionary(long ownerId)
        {
            if (_reusableOwnershipDictionary.ContainsKey(ownerId))
            {
                _reusableOwnershipDictionary[ownerId]++;
                return;
            }
            _reusableOwnershipDictionary.Add(ownerId, 1);
        }

        private long CalculateOwnerFromOwnershipDictionary()
        {
            long ownerId = 0;
            var count = 0;
            
            foreach (var id in _reusableOwnershipDictionary)
            {
                if (id.Value <= count) continue;
                ownerId = id.Key;
                count = id.Value;
            }

            return ownerId;
        }

        private void SetGridGroupOwnership(long ownerId, ReusableCubeGridList<IMyCubeGrid> reusableGridCollection)
        {
            foreach (var myCubeGrid in reusableGridCollection)
            {
                Grid grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                grid.GridOwnershipController.SetOwnership(ownerId);
            }
            _mediator.GridGroupCollectionController.Return(reusableGridCollection);
        }
    }
}