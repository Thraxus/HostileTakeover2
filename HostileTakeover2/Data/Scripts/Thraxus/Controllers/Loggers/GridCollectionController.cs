using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Models.Loggers;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridCollectionController : BaseLoggingClass
    {
        private readonly Dictionary<long, Grid> _grids = new Dictionary<long, Grid>();

        public Grid GetGrid(long entityId) => !_grids.ContainsKey(entityId) ? null : _grids[entityId];

        public void AddToGrids(long entityId, Grid grid)
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