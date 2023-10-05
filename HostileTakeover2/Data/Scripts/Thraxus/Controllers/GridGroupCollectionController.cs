using System.Collections.Generic;
using HostileTakeover2.Thraxus.Models;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GridGroupCollectionController
    {
        private readonly Dictionary<long, Grid> _grids = new Dictionary<long, Grid>();

        public Grid GetGrid(long entityId) => !_grids.ContainsKey(entityId) ? null : _grids[entityId];

        public void AddToGrids(long entityId, Grid grid)
        {
            if (_grids.ContainsKey(entityId)) return;
            _grids.Add(entityId, grid);
        }

        public void RemoveFromGrids(long entityId)
        {
            if (!_grids.ContainsKey(entityId)) return;
            _grids.Remove(entityId);
        }
    }
}