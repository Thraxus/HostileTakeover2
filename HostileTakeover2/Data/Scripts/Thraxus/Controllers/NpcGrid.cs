using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class NpcGrid : BaseGrid
    {
        public override void RegisterEvents()
        {
            WriteGeneral(nameof(RegisterEvents), $"Registering Events for a NpcGrid [{(ThisGrid == null).ToSingleChar()}] [{ThisGrid?.EntityId.ToEntityIdFormat()}]");
            if (ThisGrid == null) return;
            ThisGridController.BlockTypeController.AddGrid(ThisGrid);
            ThisGrid.OnFatBlockAdded += FatBlockAdded;
            ThisGrid.OnFatBlockRemoved += FatBlockRemoved;
            //ThisGrid.OnGridSplit += GridSplit;
            //ThisGrid.OnGridMerge += GridMerged;
        }

        private void FatBlockAdded(MyCubeBlock block)
        {
            WriteGeneral(nameof(FatBlockAdded), $"FatBlock [{block.EntityId.ToEntityIdFormat()}] added to a NpcGrid [{ThisGrid.EntityId.ToEntityIdFormat()}]");
            var connector = block as IMyShipConnector;
            if (connector != null && connector.IsFunctional && connector.IsConnected) return; // maybe this works?  
            ThisGridController.Mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => ThisGridController.BlockTypeController.AddBlock(block));
        }

        private void FatBlockRemoved(MyCubeBlock block)
        {
            WriteGeneral(nameof(FatBlockRemoved), $"FatBlock [{block.EntityId.ToEntityIdFormat()}] removed on a NpcGrid [{(ThisGrid == null).ToSingleChar()}] [{ThisGrid?.EntityId.ToEntityIdFormat()}]");
            ThisGridController.BlockTypeController.RemoveBlock(block);
            //ThisGridController.Mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => ThisGridController.BlockTypeController.RemoveBlock(block));
        } 

        public override void DeRegisterEvents()
        {
            WriteGeneral(nameof(DeRegisterEvents), $"DeRegistering Events for a NpcGrid [{(ThisGrid == null).ToSingleChar()}] [{ThisGrid?.EntityId.ToEntityIdFormat()}]");
            if (ThisGrid == null) return;
            ThisGrid.OnFatBlockRemoved -= FatBlockRemoved;
            ThisGrid.OnFatBlockAdded -= FatBlockAdded;
            //ThisGrid.OnGridSplit -= GridSplit;
            //ThisGrid.OnGridMerge -= GridMerged;
        }

        private void GridMerged(MyCubeGrid newGrid, MyCubeGrid oldGrid)
        {
            WriteGeneral(nameof(GridMerged), $"Grid Merge -- Old: [{oldGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
            ThisGridController.BlockTypeController.AddGrid(oldGrid);
        }

        private void GridSplit(MyCubeGrid oldGrid, MyCubeGrid newGrid)
        {
            WriteGeneral(nameof(GridSplit), $"Grid Split -- Old: [{newGrid.EntityId.ToEntityIdFormat()}]  New: [{newGrid.EntityId.ToEntityIdFormat()}]");
            ThisGridController.BlockTypeController.RemoveGrid(newGrid);
        }
    }
}