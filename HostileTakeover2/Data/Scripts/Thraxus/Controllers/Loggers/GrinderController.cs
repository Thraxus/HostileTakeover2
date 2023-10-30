using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Enums;
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
    public class GrinderController : BaseLoggingClass
    {
        private readonly Mediator _mediator;
        private readonly List<MyEntity> _reusableEntityList = new List<MyEntity>();

        public GrinderController(Mediator mediator)
        {
            _mediator = mediator;
        }

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

        public GridController FilterToNearestGrid(Vector3D source)
        {
            double distance = double.MaxValue;
            GridController closestGrid = null;
            foreach (var entity in _reusableEntityList)
            {
                GridController grid = _mediator.GridCollectionController.GetGrid(entity.EntityId);
                if (grid.GridOwnership.OwnerType != OwnerType.Npc)
                {
                    //WriteGeneral(nameof(FilterToNearestGrid), $"Grid has ownership type: {grid.GridOwnership.OwnerType} [{grid.EntityId:D18}]");
                    continue;
                }
                double abs = Math.Abs(((IMyCubeGrid)entity).GetPosition().LengthSquared() - source.LengthSquared());
                Common.Utilities.Statics.Statics.AddGpsLocation(((IMyCubeGrid)entity).EntityId.ToEntityIdFormat(), ((IMyCubeGrid)entity).GetPosition());
                //WriteGeneral(nameof(FilterToNearestGrid), $"Validating possible grid as target: [{(abs > distance).ToSingleChar()}] [{abs:##.###}] [{distance:E3}] [{grid.EntityId:D18}]");
                if (abs > distance) continue;
                distance = abs;
                closestGrid = grid;
            }
            return closestGrid;
        }

        public void RunGrinderLogic(IMyAngleGrinder grinder)
        {
            //WriteGeneral(nameof(RunGrinderLogic), $"Running: [{grinder.EntityId:D18}]");
            if (grinder.OwnerIdentityId == 0)
            {
                //WriteGeneral(nameof(RunGrinderLogic), $"Grinder was unowned!");
                return;
            }
            GrabAllNearbyGrids(grinder.GetPosition());
            //WriteGeneral(nameof(RunGrinderLogic), $"Grids Grabbed [{_reusableEntityList.Count:D2}]");
            var grid = FilterToNearestGrid(grinder.GetPosition());
            //WriteGeneral(nameof(RunGrinderLogic), $"Found: [{grid.EntityId:D18}]");
            _mediator.HighlightController.EnableHighlights(grid.MyGridGroupData, grinder.OwnerIdentityId);
        }
    }
}