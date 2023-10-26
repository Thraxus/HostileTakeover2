using System.Collections.Generic;
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
    internal class Mediator : BaseLoggingClass
    {
        private readonly HashSet<ICommon> _commons = new HashSet<ICommon>();
        public readonly ActionQueue ActionQueue = new ActionQueue();
        
        private readonly ObjectPool<GridController> _gridPool = new ObjectPool<GridController>();
        private readonly ObjectPool<Block> _blockPool = new ObjectPool<Block>();
        private readonly ObjectPool<HighlightSettings> _highlightSettingsPool = new ObjectPool<HighlightSettings>();
        private readonly ObjectPool<ReusableHashset<IMyCubeGrid>> _reusableMyCubeGridCollectionObjectPool = new ObjectPool<ReusableHashset<IMyCubeGrid>>();

        private readonly GridFactory _gridFactory = new GridFactory();

        public readonly GridCollectionController GridCollectionController = new GridCollectionController();
        public readonly GridGroupOwnerTypeCoordinationController GridGroupOwnerTypeCoordinationController = new GridGroupOwnerTypeCoordinationController();
        public readonly GridGroupOwnershipCoordinationController GridGroupOwnershipCoordinationController = new GridGroupOwnershipCoordinationController();
        public readonly GridGroupOwnershipTypeCoordinationController GridGroupOwnershipTypeCoordinationController = new GridGroupOwnershipTypeCoordinationController();
        public readonly GrinderController GrinderController = new GrinderController();
        public readonly HighlightController HighlightController = new HighlightController();
        public SettingsController SettingsController;

        public DefaultSettings DefaultSettings => SettingsController.DefaultSettings;

        public Mediator()
        {
            RegisterCommonEvents(GridCollectionController);
            RegisterCommonEvents(GridGroupOwnerTypeCoordinationController);
            RegisterCommonEvents(GridGroupOwnershipCoordinationController);
            RegisterCommonEvents(GridGroupOwnershipTypeCoordinationController);
            RegisterCommonEvents(GrinderController);
            RegisterCommonEvents(HighlightController);
            GridGroupOwnershipCoordinationController.Init(this);
            GridGroupOwnershipTypeCoordinationController.Init(this);
            HighlightController.Init(this);
            GrinderController.Init(this);
        }

        public void AddSettings(SettingsController settingsController)
        {
            SettingsController = settingsController;
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
                common.OnWriteToLog -= WriteGeneral;
            }
        }

        public override void Close()
        {
            DeRegisterCommonEvents();
            base.Close();
        }

        #region The methods below can be deleted before release. They are for Debug only

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
            WriteGeneral(nameof(Mediator), $"Get -- Lending a BaseGrid {_gridFactory}");
            return grid;
        }

        public void ReturnGrid(BaseGrid grid)
        {
            _gridFactory.ReturnGrid(grid);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a BaseGrid {_gridFactory}");
        }

        #endregion
    }
}