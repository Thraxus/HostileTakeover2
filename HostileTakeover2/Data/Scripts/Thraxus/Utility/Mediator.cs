using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Controllers.Loggers;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility.UserConfig.Controllers;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;

namespace HostileTakeover2.Thraxus.Utility
{
    internal class Mediator : BaseLoggingClass
    {
        private readonly HashSet<ICommon> _commons = new HashSet<ICommon>();
        public readonly ActionQueue ActionQueue = new ActionQueue();
        
        private readonly ObjectPool<Grid> GridPool = new ObjectPool<Grid>(() => new Grid());
        private readonly ObjectPool<Block> BlockPool = new ObjectPool<Block>(() => new Block());
        private readonly ObjectPool<HighlightSettings> HighlightSettingsPool = new ObjectPool<HighlightSettings>(() => new HighlightSettings());

        public readonly GridCollectionController GridCollectionController = new GridCollectionController();
        public readonly GridGroupCollectionController GridGroupCollectionController = new GridGroupCollectionController();
        public readonly GridGroupCoordinationController GridGroupCoordinationController = new GridGroupCoordinationController();
        public readonly GrinderController GrinderController = new GrinderController();
        public readonly HighlightController HighlightController = new HighlightController();
        public SettingsController SettingsController;

        public DefaultSettings DefaultSettings => SettingsController.DefaultSettings;

        public Mediator()
        {
            RegisterCommonEvents(GridGroupCoordinationController);
            RegisterCommonEvents(GridGroupCollectionController);
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

        public Grid GetGrid()
        {
            Grid grid = GridPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a Grid [{(grid == null).ToSingleChar()}] [{GridPool.Count():D3}]");
            grid.OnWriteToLog += WriteGeneral;
            return grid;
        }

        public void ReturnGrid(Grid grid)
        {
            grid.OnWriteToLog -= WriteGeneral;
            GridPool.Return(grid);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a Grid [{GridPool.Count():D3}]");
        }

        public Block GetBlock()
        {
            Block block = BlockPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a Block [{(block == null).ToSingleChar()}] [{BlockPool.Count():D3}]");
            block.OnWriteToLog += WriteGeneral;
            return block;
        }

        public void ReturnBlock(Block block)
        {
            block.OnWriteToLog -= WriteGeneral;
            BlockPool.Return(block);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a Block [{BlockPool.Count():D3}]");
        }

        public HighlightSettings GetHighlightSetting()
        {
            HighlightSettings highlightSettings = HighlightSettingsPool.Get();
            WriteGeneral(nameof(Mediator), $"Get -- Lending a HighlightSetting [{(highlightSettings == null).ToSingleChar()}] [{HighlightSettingsPool.Count():D3}]");
            return highlightSettings;
        }

        public void ReturnHighlightSetting(HighlightSettings highlightSettings)
        {
            HighlightSettingsPool.Return(highlightSettings);
            WriteGeneral(nameof(Mediator), $"Return -- Returning a HighlightSetting [{HighlightSettingsPool.Count():D3}]");
        }

        #endregion
    }
}