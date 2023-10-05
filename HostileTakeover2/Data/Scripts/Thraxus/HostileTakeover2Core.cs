using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Enums;
using HostileTakeover2.Thraxus.Common.Factions.Models;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Controller;
using HostileTakeover2.Thraxus.Utility.UserConfig.Settings;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
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
        private readonly Mediator _utilities = new Mediator();
        private SettingsController _settings;

        protected override void SuperEarlySetup()
        {
            base.SuperEarlySetup();
            _settings = new SettingsController(ModContext.ModName);
            _settings.Initialize();
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            var grid = entity as MyCubeGrid;
            if (grid == null) return;
            WriteGeneral("OnEntityAdd", $"[{grid.EntityId:D18}] {grid.DisplayName}");
            CheckGrid(grid);
            var grinder = entity as IMyAngleGrinder;
            if (grinder == null) return;
            _utilities.ActionQueue.Add(DefaultSettings.GrinderTickDelay, () => _utilities.GrinderController.RunGrinderLogic(grinder));
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
                    delay = DefaultSettings.EntityAddTickDelay;
                    action = () => GridFactory(grid);
                    break;
                case GridValidationType.VanillaTrade:
                    break;
            }
            if (action != null)
                _utilities.ActionQueue.Add(delay, action);
        }

        protected override void UpdateBeforeSim()
        {
            base.UpdateBeforeSim();
            _utilities.ActionQueue.Execute();
        }

        protected override void Unload()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            foreach (var common in _commonObjects)
            {
                DeRegisterCommonObject(common);
            }
            base.Unload();
        }
        
        private void GridFactory(MyCubeGrid cubeGrid)
        {
            var grid = _utilities.GridPool.Get();
            grid.Init(_utilities, cubeGrid);
            RegisterCommonObject(grid);
        }

        private void RegisterCommonObject(ICommon iCommon)
        {
            iCommon.OnWriteToLog += WriteGeneral;
            iCommon.OnClose += OnCommonClose;
            _commonObjects.Add(iCommon);
        }

        private void DeRegisterCommonObject(ICommon iCommon)
        {
            iCommon.OnWriteToLog -= WriteGeneral;
            iCommon.OnClose -= OnCommonClose;
            _commonObjects.Remove(iCommon);
        }

        private void OnCommonClose(IClose iClose)
        {
            DeRegisterCommonObject((ICommon)iClose);
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
            WriteGeneral(nameof(WriteRejectionReason), $"Grid Rejected as {reason}: [{grid?.EntityId:D18}] {grid?.DisplayName}");
        }
    }
}