using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class NpcGrid : BaseGrid
    {
        public override void RegisterEvents()
        {
            base.RegisterEvents();
            WriteGeneral(nameof(RegisterEvents), $"Registering Events for a NpcGrid");
            BlockTypeController.AddGrid(ThisGrid);
            ThisGrid.OnFatBlockAdded += FatBlockAdded;
            ThisGrid.OnGridSplit += GridSplit;
            ThisGrid.OnGridMerge += GridMerged;
        }
        
        public override void DeRegisterEvents()
        {
            base.DeRegisterEvents();
            BlockTypeController.Reset();
            ThisGrid.OnFatBlockAdded -= FatBlockAdded;
            ThisGrid.OnGridSplit -= GridSplit;
            ThisGrid.OnGridMerge -= GridMerged;
        }

        private void GridMerged(MyCubeGrid newGrid, MyCubeGrid oldGrid)
        {
            WriteGeneral(nameof(GridMerged), $"Grid Merge -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
            BlockTypeController.AddGrid(oldGrid);
        }

        private void GridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        {
            BlockTypeController.RemoveGrid(oldGrid);
            WriteGeneral(nameof(GridSplit), $"Grid Split -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
        }

        private void FatBlockAdded(MyCubeBlock block)
        {
            var connector = block as IMyShipConnector;
            if (connector != null && connector.IsFunctional && connector.IsConnected) return; // maybe this works?  
            ThisGridController.Mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => BlockTypeController.AddBlock(block));
        }
    }
}