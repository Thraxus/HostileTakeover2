using System.Collections.Generic;
using HostileTakeover2.Thraxus.Collections;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Settings;
using HostileTakeover2.Thraxus.Utility.UserConfig.Controllers;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Infrastructure
{
    internal class Mediator : BaseLoggingClass
    {
        private readonly HashSet<ICommon> _commons = new HashSet<ICommon>();
        public readonly ActionQueue ActionQueue = new ActionQueue();

        private readonly ObjectPool<Construct> _constructPool = new ObjectPool<Construct>(() => new Construct());
        private readonly ObjectPool<Block> _blockPool = new ObjectPool<Block>(() => new Block());
        private readonly ObjectPool<HighlightSettings> _highlightSettingsPool = new ObjectPool<HighlightSettings>(() => new HighlightSettings());
        private readonly ObjectPool<ReusableCubeGridList<IMyCubeGrid>> _reusableMyCubeGridCollectionObjectPool =
            new ObjectPool<ReusableCubeGridList<IMyCubeGrid>>(() => new ReusableCubeGridList<IMyCubeGrid>());

        public readonly ConstructController ConstructController = new ConstructController();
        public readonly GrinderController GrinderController = new GrinderController();
        public readonly HighlightController HighlightController = new HighlightController();
        public UserConfigController UserConfigController;

        public DefaultSettings DefaultSettings => UserConfigController.DefaultSettings;

        public Mediator()
        {
            RegisterCommonEvents(ConstructController);
            RegisterCommonEvents(GrinderController);
            RegisterCommonEvents(HighlightController);
            HighlightController.Init(this);
            GrinderController.Init(this);
        }

        public void AddSettings(UserConfigController settingsController)
        {
            UserConfigController = settingsController;
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

        public Construct GetConstruct(long entityId)
        {
            Construct construct = _constructPool.Get();
            construct.OnWriteToLog += WriteGeneral;
            return construct;
        }

        public void ReturnConstruct(Construct construct, long entityId)
        {
            construct.OnWriteToLog -= WriteGeneral;
            _constructPool.Return(construct);
        }

        public Block GetBlock(long blockId)
        {
            Block block = _blockPool.Get();
            block.OnWriteToLog += WriteGeneral;
            return block;
        }

        public void ReturnBlock(Block block, long blockId)
        {
            block.OnWriteToLog -= WriteGeneral;
            _blockPool.Return(block);
        }

        public HighlightSettings GetHighlightSetting()
        {
            return _highlightSettingsPool.Get();
        }

        public void ReturnHighlightSetting(HighlightSettings highlightSettings)
        {
            _highlightSettingsPool.Return(highlightSettings);
        }

        public ReusableCubeGridList<IMyCubeGrid> GetReusableCubeGridList(IMyGridGroupData myGridGroupData)
        {
            var list = _reusableMyCubeGridCollectionObjectPool.Get();
            myGridGroupData.GetGrids(list);
            return list;
        }

        public void ReturnReusableCubeGridList(ReusableCubeGridList<IMyCubeGrid> list)
        {
            _reusableMyCubeGridCollectionObjectPool.Return(list);
        }
    }
}