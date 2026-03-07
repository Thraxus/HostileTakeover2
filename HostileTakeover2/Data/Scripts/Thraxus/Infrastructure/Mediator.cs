using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Settings;
using HostileTakeover2.Thraxus.Utility.Classification;
using HostileTakeover2.Thraxus.Utility.UserConfig.Controllers;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.ModAPI;
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

        private readonly HashSet<long> _npcIdentities = new HashSet<long>();

        public bool IsNpcIdentity(long id) { return _npcIdentities.Contains(id); }

        public void BuildNpcIdentityCache()
        {
            var identityList = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identityList);
            foreach (var identity in identityList)
            {
                if (MyAPIGateway.Players.TryGetSteamId(identity.IdentityId) == 0)
                    _npcIdentities.Add(identity.IdentityId);
            }
            WriteGeneral(nameof(BuildNpcIdentityCache), $"NPC identity cache built: {_npcIdentities.Count} entries.");
        }

        public readonly BlockClassificationData BlockClassificationData = new BlockClassificationData();
        public readonly ConstructController ConstructController = new ConstructController();
        public readonly GrinderController GrinderController = new GrinderController();
        public readonly HighlightController HighlightController = new HighlightController();
        public UserConfigController UserConfigController;

        public DefaultSettings DefaultSettings => UserConfigController.DefaultSettings;

        public Mediator()
        {
            ActionQueue.OnReport += WriteGeneral;
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
            ActionQueue.OnReport -= WriteGeneral;
            DeRegisterCommonEvents();
            base.Close();
        }

        private bool IsPoolLoggingActive => UserConfigController != null && DefaultSettings.IsVerboseActiveFor(DebugType.Pool);

        public Construct GetConstruct(long entityId)
        {
            Construct construct = _constructPool.Get();
            construct.OnWriteToLog += WriteGeneral;
            if (IsPoolLoggingActive) WriteGeneral(DebugType.Pool, nameof(GetConstruct), $"Construct retrieved from pool for [{entityId:D18}]");
            return construct;
        }

        public void ReturnConstruct(Construct construct)
        {
            if (IsPoolLoggingActive) WriteGeneral(DebugType.Pool, nameof(ReturnConstruct), $"Construct returned to pool [{construct.EntityId:D18}]");
            construct.OnWriteToLog -= WriteGeneral;
            _constructPool.Return(construct);
        }

        public Block GetBlock(long blockId)
        {
            Block block = _blockPool.Get();
            block.OnWriteToLog += WriteGeneral;
            if (IsPoolLoggingActive) WriteGeneral(DebugType.Pool, nameof(GetBlock), $"Block retrieved from pool for [{blockId:D18}]");
            return block;
        }

        public void ReturnBlock(Block block)
        {
            if (IsPoolLoggingActive) WriteGeneral(DebugType.Pool, nameof(ReturnBlock), $"Block returned to pool [{block.EntityId:D18}]");
            block.OnWriteToLog -= WriteGeneral;
            _blockPool.Return(block);
        }

        public HighlightSettings GetHighlightSetting()
        {
            if (IsPoolLoggingActive) WriteGeneral(DebugType.Pool, nameof(GetHighlightSetting), "HighlightSettings retrieved from pool");
            return _highlightSettingsPool.Get();
        }

        public void ReturnHighlightSetting(HighlightSettings highlightSettings)
        {
            if (IsPoolLoggingActive) WriteGeneral(DebugType.Pool, nameof(ReturnHighlightSetting), "HighlightSettings returned to pool");
            _highlightSettingsPool.Return(highlightSettings);
        }

    }
}