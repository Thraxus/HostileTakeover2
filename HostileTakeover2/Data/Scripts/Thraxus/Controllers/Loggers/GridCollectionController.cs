using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridCollectionController : BaseLoggingClass
    {
        private readonly Dictionary<long, GridController> _grids = new Dictionary<long, GridController>();

        public GridController GetGrid(long entityId) => !_grids.ContainsKey(entityId) ? null : _grids[entityId];

        public void AddToGrids(long entityId, GridController grid)
        {
            if (_grids.ContainsKey(entityId)) return;
            WriteGeneral(nameof(AddToGrids), $"Adding Grid with EntityId: {entityId:D18}");
            _grids.Add(entityId, grid);
        }

        public void RemoveFromGrids(long entityId)
        {
            if (!_grids.ContainsKey(entityId)) return;
            WriteGeneral(nameof(AddToGrids), $"Removing Grid with EntityId: {entityId:D18}");
            _grids.Remove(entityId);
        }
    }
}