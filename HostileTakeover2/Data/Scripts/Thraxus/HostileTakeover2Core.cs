using System.Collections.Generic;
using System.Text;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Enums;
using HostileTakeover2.Thraxus.Common.Factions.Models;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Factories;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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

        private readonly ConstructFactory _constructFactory;
        private readonly HashSet<ICommon> _commonObjects = new HashSet<ICommon>();
        public readonly ActionQueue ActionQueue = new ActionQueue();



        public HostileTakeover2Core()
        {
            _constructFactory = new ConstructFactory(ActionQueue);
        }

        /// <inheritdoc />
        protected override void LateSetup()
        {
            base.LateSetup();
            var sb = new StringBuilder();

            WriteGeneral("LateSetup",sb.ToString());
        }

        protected override void SuperEarlySetup()
        {
            base.SuperEarlySetup();
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            var grid = entity as MyCubeGrid;
            if (grid == null) return;
            WriteGeneral("OnEntityAdd", $"[{grid.EntityId:D18}] {grid.DisplayName}");
            if (ValidateGrid(grid))
                ActionQueue.Add(2, () => ConstructFactory(grid));
        }

        protected override void UpdateBeforeSim()
        {
            base.UpdateBeforeSim();
            ActionQueue.Execute();
        }

        private void OnGridSplitCreated(MyCubeGrid grid)
        {
            WriteGeneral("OnGridSplitCreated", $"[{grid.EntityId:D18}] {grid.DisplayName}");
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

        private readonly Dictionary<long, ConstructController> _constructMap = new Dictionary<long, ConstructController>();
        
        private void ConstructFactory(MyCubeGrid grid)
        {
            if (_constructMap.ContainsKey(grid.EntityId))
            {
                WriteGeneral(nameof(ConstructFactory), $"[{grid.EntityId:D18}] Grid rejected.  It already exists in the dictionary under Construct [{_constructMap[grid.EntityId].ConstructId:D5}]");
                return;
            }

            ConstructController constructController = _constructFactory.SetupNewConstruct(grid);
            RegisterCommonObject(constructController);
            foreach (var id in constructController.GridIds)
            {
                if (_constructMap.ContainsKey(id))
                {
                    WriteGeneral(nameof(ConstructFactory), $"[{id:D18}] Duplicate call for key insertion.");
                    continue;
                }
                //WriteGeneral(nameof(ConstructFactory), $"[{id:D18}] Adding to Construct {constructController.ConstructId:D5}.");
                _constructMap.Add(id, constructController);
            }

            PrintConstructMap();
        }

        private void RegisterCommonObject(ICommon iCommon)
        {
            iCommon.OnWriteToLog += WriteGeneral;
            iCommon.OnClose += OnClose;
            iCommon.OnReset += OnReset;
            _commonObjects.Add(iCommon);
        }

        private void OnReset(IReset iReset)
        {
            OnClose((IClose)iReset);
        }

        private void DeRegisterCommonObject(ICommon iCommon)
        {
            iCommon.OnWriteToLog -= WriteGeneral;
            iCommon.OnClose -= OnClose;
            iCommon.OnReset -= OnReset;
            _commonObjects.Remove(iCommon);
        }

        private void OnClose(IClose iClose)
        {
            DeRegisterCommonObject((ICommon)iClose);
        }

        private void PrintConstructMap()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (var construct in _constructMap)
            {
                sb.AppendFormat("{0,-8}[{1:D18}] is a member of Construct {2:D5}\n", " ", construct.Key, construct.Value.ConstructId);
            }
            WriteGeneral(nameof(PrintConstructMap), sb.ToString());
        }

        private bool ValidateGrid(MyCubeGrid grid)
        {
            // I don't exist!  So why am I here...
            if (grid == null)
            {
                WriteRejectionReason(null, "NULL");
                return false;
            }

            // You don't own me.  No one owns me! 
            if (grid.BigOwners == null || grid.BigOwners.Count == 0)
                return false;

            // I'm a projection!  Begone fool! ...or lend me your... components.
            if (grid.Physics == null)
            {
                WriteRejectionReason(grid, "NO PHYSICS");
                return false;
            }

            // I'm not destructible because someone said so.
            if (!grid.DestructibleBlocks)
            {
                WriteRejectionReason(grid, "INDESTRUCTIBLE");
                return false;
            }

            // Haha bitch, I'm immune to your garbage.
            if (grid.Immune)
            {
                WriteRejectionReason(grid, "IMMUNE");
                return false;
            }

            // Thou shall not edit me.  So saith ...me.
            if (!grid.Editable)
            {
                WriteRejectionReason(grid, "NOT EDITABLE");
                return false;
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
                        return false;
                    }
                }
            }

            return true;
        }

        private void WriteRejectionReason(MyCubeGrid grid, string reason)
        {
            WriteGeneral(nameof(WriteRejectionReason), $"Grid Rejected as {reason}: [{grid?.EntityId:D18}] {grid?.DisplayName}");
        }

    }
}