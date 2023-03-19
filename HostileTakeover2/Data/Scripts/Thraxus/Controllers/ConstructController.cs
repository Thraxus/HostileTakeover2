using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class ConstructController : BaseLoggingClass, IActionQueue
    {
        public ActionQueue ActionQueue { get; set; }

        public int ConstructId;
        private List<MyCubeGrid> _grids;
        public List<long> GridIds;
        
        public void Initialize(int constructId, ActionQueue actionQueue)
        {
            ConstructId = constructId;
            ActionQueue = actionQueue;
            _grids = new List<MyCubeGrid>();
            GridIds = new List<long>();
            WriteGeneral("Initialize", $"[{ConstructId:D5}] Construct Initialized.");
        }

        public override void Reset()
        {
            ConstructId = 0;
            ActionQueue = null;
            _grids.Clear();
            GridIds.Clear();
            base.Reset();
        }

        public void AddGrids(MyCubeGrid grids)
        {
            grids.GetConnectedGrids(GridLinkTypeEnum.Mechanical, _grids);
            foreach (var grid in _grids)
            {
                GridIds.Add(grid.EntityId);
            }
        }

        private void RemoveGrid(long id)
        {
            if (!GridIds.Contains(id)) return;
            for (int i = _grids.Count - 1; i >= 0; i--)
            {
                if (_grids[i].EntityId != id) continue;
                _grids.RemoveAtFast(i);
                break;
            }
            GridIds.Remove(id);
        }

        public bool ContainsGrid(MyCubeGrid grid)
        {
            return GridIds.Contains(grid.EntityId);
        }
    }
}
