using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridGroupCoordinationController : BaseLoggingClass
    {
        private Mediator _mediator;
        private readonly Dictionary<long, int> _reusableOwnershipDictionary = new Dictionary<long, int>();

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }
        
        public void InitializeOwnership(IMyGridGroupData myGridGroupData)
        {
            long ownerId = GetCurrentOwnerId(myGridGroupData);
            WriteGeneral(nameof(InitializeOwnership), $"Grid group owner determined to be {ownerId:D18}");
            SetGridGroupOwnership(myGridGroupData, ownerId);
        }

        public void ReEvaluateOwnership(IMyGridGroupData myGridGroupData, long cachedOwnerId)
        {
            long ownerId = GetCurrentOwnerId(myGridGroupData);
            WriteGeneral(nameof(ReEvaluateOwnership), $"Ownership reevaluated, change required: [{(cachedOwnerId == ownerId).ToSingleChar()}]");
            if (cachedOwnerId == ownerId) return;
            SetGridGroupOwnership(myGridGroupData, ownerId);
        }

        private long GetCurrentOwnerId(IMyGridGroupData myGridGroupData)
        {
            _reusableOwnershipDictionary.Clear();
            var gridList = _mediator.GetReusableCubeGridList(myGridGroupData);

            foreach (var myCubeGrid in gridList)
            {
                Grid grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                AddToOwnershipDictionary(grid.CurrentOwnerId);
            }
            _mediator.ReturnReusableCubeGridList(gridList);
            return CalculateOwnerFromOwnershipDictionary();
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

        private void SetGridGroupOwnership(IMyGridGroupData myGridGroupData, long ownerId)
        {
            var gridList = _mediator.GetReusableCubeGridList(myGridGroupData);
            foreach (var myCubeGrid in gridList)
            {
                Grid grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                grid.GridOwnershipController.SetOwnership(ownerId);
            }
            _mediator.ReturnReusableCubeGridList(gridList);
        }

        public override void Reset()
        {
            _reusableOwnershipDictionary.Clear();
            base.Reset();
        }
    }
}