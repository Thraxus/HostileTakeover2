using HostileTakeover2.Thraxus.Common;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Enums;
using HostileTakeover2.Thraxus.Common.Factions.Models;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.Classification;
using HostileTakeover2.Thraxus.Utility.UserConfig.Controllers;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Infrastructure;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace HostileTakeover2.Thraxus
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, priority: int.MinValue + 1)]
    internal class HostileTakeover2Core : BaseSessionComp
    {
        protected override string CompName => nameof(HostileTakeover2Core);
        protected override CompType Type => CompType.Server;
        protected override MyUpdateOrder Schedule => MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation;

        private readonly HashSet<ICommon> _commonObjects = new HashSet<ICommon>();
        private readonly Mediator _mediator = new Mediator();
        private UserConfigController _userConfigController;

        protected override void EarlyInit()
        {
            DebugType.Initialize();
        }

        protected override void SuperEarlySetup()
        {
            base.SuperEarlySetup();
            _userConfigController = new UserConfigController(ModContext.ModName);
            if (References.IsServer)
            {
                // Route settings and mediator log output up to the session-level log.
                _userConfigController.OnWriteToLog += WriteGeneral;
                _userConfigController.InitializeServer();
                _mediator.OnWriteToLog += WriteGeneral;
                _mediator.AddSettings(_userConfigController);
                BlockClassifier.Populate(_mediator.BlockClassificationData, _userConfigController.DefaultSettings);
                BlockClassificationWriter.Write(_mediator.BlockClassificationData);
                BlockClassificationOverridesReader.Read(_mediator.BlockClassificationData);
                MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            }
            else
            {
                _userConfigController.InitializeClient();
            }
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            _mediator.ActionQueue.Add(DefaultSettings.EntityAddTickDelay, () =>
            {
                if (!CheckForGrid(entity)) CheckForGrinder(entity);
            });
        }

        private bool CheckForGrid(IMyEntity entity)
        {
            var grid = entity as MyCubeGrid;
            if (grid == null) return false;
            CheckGrid(grid);
            return true;
        }

        private bool CheckForGrinder(IMyEntity entity)
        {
            var grinder = entity as IMyAngleGrinder;
            if (grinder == null) return false;
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Grinder))
                WriteGeneral(nameof(CheckForGrinder), $"Grinder: [{grinder.EntityId:D18}]");
            _mediator.GrinderController.RunGrinderLogic(grinder);
            return true;
        }

        private void CheckGrid(MyCubeGrid grid)
        {
            GridValidationType type = ValidateGrid(grid);
            Action action = null;
            var delay = 0;
            switch (type)
            {
                case GridValidationType.Indestructible:
                case GridValidationType.Immune:
                case GridValidationType.NotEditable:
                    delay = DefaultSettings.RecheckGridInterval;
                    action = () => CheckGrid(grid);
                    break;
                case GridValidationType.NoPhysics:
                    break;
                case GridValidationType.Null:
                    break;
                case GridValidationType.Valid:
                    delay = DefaultSettings.MinorTickDelay;
                    action = () => ConstructFactory(grid);
                    break;
                case GridValidationType.VanillaTrade:
                    break;
            }
            if (action != null)
                _mediator.ActionQueue.Add(delay, action);
        }

        public override void BeforeStart()
        {
            base.BeforeStart();
            if (!References.IsServer) return;
            WriteGeneral(nameof(BeforeStart), _userConfigController.DefaultSettings.PrintSettings().ToString());
        }

        protected override void LateSetup()
        {
            base.LateSetup();
        }

        protected override void UpdateBeforeSim()
        {
            base.UpdateBeforeSim();
            _mediator.ActionQueue.Execute();
        }

        protected override void Unload()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            try { _mediator.Close(); }
            catch (Exception e) { WriteGeneral(nameof(Unload), $"Exception: {e}"); }
            _mediator.OnWriteToLog -= WriteGeneral;
            base.Unload();
        }
        
        private void ConstructFactory(MyCubeGrid cubeGrid)
        {
            var construct = _mediator.GetConstruct(cubeGrid.EntityId);
            construct.Init(_mediator, cubeGrid);
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Construct))
                WriteGeneral(nameof(ConstructFactory), $"Construct Factory Engaged.  Created Construct.");
        }

        private GridValidationType ValidateGrid(MyCubeGrid grid)
        {
            // I don't exist!  So why am I here...
            if (grid == null)
            {
                WriteRejectionReason(null, "NULL");
                return GridValidationType.Null;
            }

            // I'm a projection!  Begone fool! ...or lend me your... components.
            if (grid.Physics == null)
            {
                WriteRejectionReason(grid, "NO PHYSICS");
                return GridValidationType.NoPhysics;
            }

            // I'm not destructible because someone said so.
            if (!grid.DestructibleBlocks)
            {
                WriteRejectionReason(grid, "INDESTRUCTIBLE");
                return GridValidationType.Indestructible;
            }

            // Haha bitch, I'm immune to your garbage.
            if (grid.Immune)
            {
                WriteRejectionReason(grid, "IMMUNE");
                return GridValidationType.Immune;
            }

            // Thou shall not edit me.  So saith ...me.
            if (!grid.Editable)
            {
                WriteRejectionReason(grid, "NOT EDITABLE");
                return GridValidationType.NotEditable;
            }

            // I'm a station...!
            if (grid.IsStatic)
            {
                // ...that has an owner...!
                if (grid.BigOwners.Count > 0)
                {
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
                    if (faction != null && FactionDictionaries.VanillaTradeFactions.ContainsKey(faction.FactionId))
                    {
                        // ...that belongs to cheater NPC's, so back off! 
                        WriteRejectionReason(grid, "VANILLA TRADE");
                        return GridValidationType.VanillaTrade;
                    }
                }
            }

            return GridValidationType.Valid;
        }

        private void WriteRejectionReason(MyCubeGrid grid, string reason)
        {
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Grid))
                WriteGeneral(nameof(WriteRejectionReason), $"Grid Rejected as {reason}: [{grid?.EntityId:D18}] {grid?.DisplayName}");
        }
    }
}