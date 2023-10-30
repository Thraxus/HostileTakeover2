using System.Collections.Generic;
using System.Text;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Controllers.Loggers;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Factories;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility.UserConfig.Controllers;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Utility
{
    public class Mediator : BaseLoggingClass
    {
        private readonly HashSet<ICommon> _commons = new HashSet<ICommon>();
        public readonly ActionQueue ActionQueue = new ActionQueue();

        private readonly ObjectPool<Block> _blockPool = new ObjectPool<Block>();
        private readonly ObjectPool<GridController> _gridPool = new ObjectPool<GridController>();
        private readonly ObjectPool<HighlightSettings> _highlightSettingsPool = new ObjectPool<HighlightSettings>();
        private readonly ObjectPool<ReusableHashset<IMyCubeGrid>> _reusableMyCubeGridCollectionObjectPool = new ObjectPool<ReusableHashset<IMyCubeGrid>>();

        private readonly GridFactory _gridFactory = new GridFactory();
        public readonly GridGroupController GridGroupController;

        public readonly GridCollectionController GridCollectionController;
        public readonly GridGroupOwnershipTypeCoordinationController GridGroupOwnershipTypeCoordinationController;
        public readonly GrinderController GrinderController;
        public readonly HighlightController HighlightController;
        public SettingsController SettingsController;

        public DefaultSettings DefaultSettings => SettingsController.DefaultSettings;

        public Mediator(SettingsController settingsController)
        {
            SettingsController = settingsController;
            
            var entityController = new EntityController(this);
            RegisterCommonEvents(entityController);

            GridCollectionController = new GridCollectionController();
            RegisterCommonEvents(GridCollectionController);

            GridGroupOwnershipTypeCoordinationController = new GridGroupOwnershipTypeCoordinationController(this);
            RegisterCommonEvents(GridGroupOwnershipTypeCoordinationController);

            GrinderController = new GrinderController(this);
            RegisterCommonEvents(GrinderController);

            HighlightController = new HighlightController(this);
            RegisterCommonEvents(HighlightController);

            GridGroupController = new GridGroupController(this);
            RegisterCommonEvents(GridGroupController);

            ActionQueue.Add(1, InitGridGroupController);
        }

        private void InitGridGroupController()
        {
            GridGroupController.Init();
            WriteGeneral(nameof(Mediator), "GridGroupController online");
        }

        private void RegisterCommonEvents(ICommon common)
        {
            common.OnWriteToLog += WriteGeneral;
            _commons.Add(common);
        }

        private void DeRegisterCommonEvents()
        {
            foreach (var common in _commons)
            {
                WriteGeneral(nameof(DeRegisterCommonEvents), $"DeRegistering {common.GetType()}");
                common.Close();
                common.OnWriteToLog -= WriteGeneral;
            }
            WriteGeneral(nameof(DeRegisterCommonEvents), $"DeRegistering complete.");
        }

        public override void Close()
        {
            base.Close();
            ActionQueue.Reset();
            DeRegisterCommonEvents();
            var sb = new StringBuilder();
            sb.AppendLine("Pool Stats:");
            sb.AppendLine(_blockPool.ToString());
            sb.AppendLine(_gridPool.ToString());
            sb.AppendLine(_highlightSettingsPool.ToString());
            sb.AppendLine(_reusableMyCubeGridCollectionObjectPool.ToString());
            sb.AppendLine(_gridFactory.ToString());
            WriteGeneral(nameof(Close), sb.ToString());
        }

        #region The methods below deal with get's and returns for the various pools

        public GridController GetGridController(long entityId)
        {
            GridController grid = _gridPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a GridController [{entityId.ToEntityIdFormat()}] {_gridPool}");
            grid.OnWriteToLog += WriteGeneral;
            return grid;
        }

        public void ReturnGridController(GridController grid, long entityId)
        {
            grid.OnWriteToLog -= WriteGeneral;
            _gridPool.Return(grid);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a GridController [{entityId.ToEntityIdFormat()}] {_gridPool}");
        }

        public Block GetBlock(long blockId)
        {
            Block block = _blockPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a Block [{blockId.ToEntityIdFormat()}] {_blockPool}");
            return block;
        }

        public void ReturnBlock(Block block, long blockId)
        {
            _blockPool.Return(block);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a Block [{blockId.ToEntityIdFormat()}] {_blockPool}");
        }

        public HighlightSettings GetHighlightSetting()
        {
            HighlightSettings highlightSettings = _highlightSettingsPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a HighlightSetting {_highlightSettingsPool}");
            return highlightSettings;
        }

        public void ReturnHighlightSetting(HighlightSettings highlightSettings)
        {
            _highlightSettingsPool.Return(highlightSettings);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a HighlightSetting {_highlightSettingsPool}");
        }

        public ReusableHashset<IMyCubeGrid> GetReusableMyCubeGridList(IMyGridGroupData myGridGroupData)
        {
            var list = _reusableMyCubeGridCollectionObjectPool.Get();
            myGridGroupData.GetGrids(list);
            WriteGeneral(nameof(Mediator), $"Get -- Lending a ReusableCubeGridList {_reusableMyCubeGridCollectionObjectPool}");
            return list;
        }

        public void ReturnReusableMyCubeGridList(ReusableHashset<IMyCubeGrid> list)
        {
            _reusableMyCubeGridCollectionObjectPool.Return(list);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a ReusableCubeGridList {_reusableMyCubeGridCollectionObjectPool}");
        }

        public BaseGrid GetGrid(OwnerType type)
        {
            BaseGrid grid = _gridFactory.GetGrid(type);
            grid.OnWriteToLog += WriteGeneral;
            WriteGeneral(nameof(Mediator), $"Get -- Lending a BaseGrid {_gridFactory}");
            return grid;
        }

        public void ReturnGrid(BaseGrid grid)
        {
            _gridFactory.ReturnGrid(grid);
            grid.OnWriteToLog -= WriteGeneral;
            WriteGeneral(nameof(Mediator), $"Return -- Returning a BaseGrid {_gridFactory}");
        }

        public ReusableHashset<IMyCubeGrid> GetGridGroupCollection(IMyGridGroupData myGridGroupData)
        {
            WriteGeneral(nameof(Mediator), $"Get -- Loaning a GridGroupCollection");
            return GridGroupController.GetGridGroup(myGridGroupData);
        }

        #endregion
    }
}