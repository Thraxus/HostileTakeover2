using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.Utilities.Tools.Logging;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Utility;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GridGroupCoordinationController
    {
        private Mediator _mediator;
        private readonly List<IMyCubeGrid> _reusableGridCollection = new List<IMyCubeGrid>();
        private readonly Dictionary<long, int> _reusableOwnershipDictionary = new Dictionary<long, int>();

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }

        public void CoordinateHighlights(IMyGridGroupData myGridGroupData)
        {

        }

        public void CoordinateOwnership(IMyGridGroupData myGridGroupData)
        {
            IterateGridGroup(myGridGroupData);
        }

        private void IterateGridGroup(IMyGridGroupData myGridGroupData)
        {
            _reusableGridCollection.Clear();
            _reusableOwnershipDictionary.Clear();
            myGridGroupData.GetGrids(_reusableGridCollection);

            foreach (var myCubeGrid in _reusableGridCollection)
            {
                Grid grid = _mediator.GridGroupCollectionController.GetGrid(myCubeGrid.EntityId);
                AddToOwnershipDictionary(grid.CurrentOwnerId);
            }

            long ownerId = CalculateOwnerFromOwnershipDictionary();
            SetGridGroupOwnership(ownerId);
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

        private void SetGridGroupOwnership(long ownerId)
        {
            foreach (var myCubeGrid in _reusableGridCollection)
            {
                Grid grid = _mediator.GridGroupCollectionController.GetGrid(myCubeGrid.EntityId);
                grid.GridOwnershipController.SetOwnership(ownerId);
            }
        }
    }
}