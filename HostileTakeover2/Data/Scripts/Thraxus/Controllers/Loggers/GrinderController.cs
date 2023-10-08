using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.Extensions;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GrinderController : BaseLoggingClass, IInit<Mediator>
    {
        private Mediator _mediator;

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }

        private readonly List<MyEntity> _reusableEntityList = new List<MyEntity>();

        private void GrabAllNearbyGrids(Vector3D center)
        {
            _reusableEntityList.Clear();
            var pruneSphere = new BoundingSphereD(center, _mediator.DefaultSettings.EntityDetectionRange.Current);
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref pruneSphere, _reusableEntityList);
            for (int i = _reusableEntityList.Count - 1; i >= 0; i--)
            {
                if (!(_reusableEntityList[i] is MyCubeGrid))
                    _reusableEntityList.RemoveAtFast(i);
            }
        }

        public Grid FilterToNearestGrid(Vector3D source)
        {
            double distance = double.MaxValue;
            Grid closestGrid = null;
            foreach (var entity in _reusableEntityList)
            {
                Grid grid = _mediator.GridCollectionController.GetGrid(entity.EntityId);
                if (grid.GridOwnershipController.OwnershipType != OwnershipType.Npc) continue;
                double abs = Math.Abs(((IMyCubeGrid)entity).GetPosition().LengthSquared() - source.LengthSquared());
                Common.Utilities.Statics.Statics.AddGpsLocation(((IMyCubeGrid)entity).EntityId.ToEntityIdFormat(), ((IMyCubeGrid)entity).GetPosition());
                WriteGeneral(nameof(FilterToNearestGrid), $"Validating possible grid as target: [{(abs > distance).ToSingleChar()}] [{abs:##.###}] [{distance:E3}] [{grid.EntityId:D18}]");
                if (abs > distance) continue;
                distance = abs;
                closestGrid = grid;
            }
            return closestGrid;
        }

        public void RunGrinderLogic(IMyAngleGrinder grinder)
        {
            if (grinder.OwnerIdentityId == 0) return;
            GrabAllNearbyGrids(grinder.GetPosition());
            var grid = FilterToNearestGrid(grinder.GetPosition());
            WriteGeneral(nameof(RunGrinderLogic), $"Found: [{grid.EntityId:D18}]");
            grid.TriggerHighlights(grinder.OwnerIdentityId);
            //IMyEntity entityById = MyAPIGateway.Entities.GetEntityById(grinder.OwnerId);
            //List<MyEntity> entList = GrabNearbyGrids(entityById?.GetPosition() ?? grinder.GetPosition());
            //WriteGeneral(nameof(RunGrinderLogic), $"Grinder: [{grinder.OwnerIdentityId:D18}] [{grinder.OwnerId:D18}] [{entList.Count:D2}] [{grinder.GetPosition()}] [{entityById?.GetPosition()}]");
            //Grid target = FilterToNearestGrid(grinder.GetPosition());
            //Grid grid = _mediator.GridCollectionController.GetGrid(target.EntityId);
            //WriteGeneral(nameof(RunGrinderLogic), $"Looking for: [{(grid == null ? "T" : "F")}] [{target.EntityId:D18}] [{grinder.OwnerIdentityId:D18}]");
            //if (grid == null || grid.GridOwnershipController.OwnershipType != OwnershipType.Npc) continue;
            //WriteGeneral(nameof(RunGrinderLogic), $"Found Target: [{target.EntityId:D18}]");

            //foreach (MyEntity target in entList)
            //{
            //    if (grinder.OwnerIdentityId == 0) break;
            //    Grid grid = _mediator.GridCollectionController.GetGrid(target.EntityId);
            //    WriteGeneral(nameof(RunGrinderLogic), $"Looking for: [{(grid == null ? "T" : "F")}] [{target.EntityId:D18}] [{grinder.OwnerIdentityId:D18}]");
            //    if (grid == null || grid.GridOwnershipController.OwnershipType != OwnershipType.Npc) continue;
            //    WriteGeneral(nameof(RunGrinderLogic), $"Found: [{target.EntityId:D18}]");
            //    grid.TriggerHighlights(grinder.OwnerIdentityId);
            //}
        }
    }
}