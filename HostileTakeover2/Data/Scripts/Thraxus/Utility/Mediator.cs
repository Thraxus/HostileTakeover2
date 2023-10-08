using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers.Loggers;
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
        
        private readonly ObjectPool<Grid> _gridPool = new ObjectPool<Grid>(() => new Grid());
        private readonly ObjectPool<Block> _blockPool = new ObjectPool<Block>(() => new Block());
        private readonly ObjectPool<HighlightSettings> _highlightSettingsPool = new ObjectPool<HighlightSettings>(() => new HighlightSettings());
        private readonly ObjectPool<ReusableCubeGridList<IMyCubeGrid>> _reusableMyCubeGridCollectionObjectPool =
            new ObjectPool<ReusableCubeGridList<IMyCubeGrid>>(() => new ReusableCubeGridList<IMyCubeGrid>());

        public readonly GridCollectionController GridCollectionController = new GridCollectionController();
        public readonly GridGroupCoordinationController GridGroupCoordinationController = new GridGroupCoordinationController();
        public readonly GrinderController GrinderController = new GrinderController();
        public readonly HighlightController HighlightController = new HighlightController();
        public SettingsController SettingsController;

        public DefaultSettings DefaultSettings => SettingsController.DefaultSettings;

        public Mediator()
        {
            RegisterCommonEvents(GridGroupCoordinationController);
            RegisterCommonEvents(GridCollectionController);
            RegisterCommonEvents(GrinderController);
            RegisterCommonEvents(HighlightController);
            GridGroupCoordinationController.Init(this);
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
                common.Close();
            }
        }

        public override void Close()
        {
            DeRegisterCommonEvents();
            base.Close();
        }

        #region The methods below can be deleted before release. They are for Debug only

        public Grid GetGrid(long entityId)
        {
            Grid grid = _gridPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a Grid [{entityId.ToEntityIdFormat()}] [{(grid == null).ToSingleChar()}] {_gridPool}");
            grid.OnWriteToLog += WriteGeneral;
            return grid;
        }

        public void ReturnGrid(Grid grid, long entityId)
        {
            grid.OnWriteToLog -= WriteGeneral;
            _gridPool.Return(grid);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a Grid [{entityId.ToEntityIdFormat()}] {_gridPool}");
        }

        public Block GetBlock(long blockId)
        {
            Block block = _blockPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a Block [{blockId.ToEntityIdFormat()}] [{(block == null).ToSingleChar()}] {_blockPool}");
            block.OnWriteToLog += WriteGeneral;
            return block;
        }

        public void ReturnBlock(Block block, long blockId)
        {
            block.OnWriteToLog -= WriteGeneral;
            _blockPool.Return(block);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a Block [{blockId.ToEntityIdFormat()}] {_blockPool}");
        }

        public HighlightSettings GetHighlightSetting()
        {
            HighlightSettings highlightSettings = _highlightSettingsPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a HighlightSetting [{(highlightSettings == null).ToSingleChar()}] {_highlightSettingsPool}");
            return highlightSettings;
        }

        public void ReturnHighlightSetting(HighlightSettings highlightSettings)
        {
            _highlightSettingsPool.Return(highlightSettings);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a HighlightSetting {_highlightSettingsPool}");
        }

        public ReusableCubeGridList<IMyCubeGrid> GetReusableCubeGridList(IMyGridGroupData myGridGroupData)
        {
            WriteGeneral(nameof(Mediator), $"Get -- Lending a ReusableCubeGridList {_reusableMyCubeGridCollectionObjectPool}");
            var list = _reusableMyCubeGridCollectionObjectPool.Get();
            myGridGroupData.GetGrids(list);
            return list;
        }

        public void ReturnReusableCubeGridList(ReusableCubeGridList<IMyCubeGrid> list)
        {
            WriteGeneral(nameof(Mediator), $"Return -- Returning a ReusableCubeGridList {_reusableMyCubeGridCollectionObjectPool}");
            _reusableMyCubeGridCollectionObjectPool.Return(list);
        }

        #endregion
    }
}