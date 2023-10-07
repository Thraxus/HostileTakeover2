using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game.Entity;
using VRage.ModAPI;
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

        private List<MyEntity> GrabNearbyGrids(Vector3D center)
        {
            _reusableEntityList.Clear();
            var pruneSphere = new BoundingSphereD(center, _mediator.DefaultSettings.EntityDetectionRange.Current);
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref pruneSphere, _reusableEntityList);
            for (int i = _reusableEntityList.Count - 1; i >= 0; i--)
            {
                if (!(_reusableEntityList[i] is MyCubeGrid))
                    _reusableEntityList.RemoveAtFast(i);
            }
            return _reusableEntityList;
        }

        public void RunGrinderLogic(IMyAngleGrinder grinder)
        {
            IMyEntity entityById = MyAPIGateway.Entities.GetEntityById(grinder.OwnerId);
            List<MyEntity> entList = GrabNearbyGrids(entityById?.GetPosition() ?? grinder.GetPosition());
            WriteGeneral(nameof(RunGrinderLogic), $"Grinder: [{grinder.OwnerIdentityId:D18}] [{grinder.OwnerId:D18}] [{entList.Count:D2}] [{grinder.GetPosition()}] [{entityById?.GetPosition()}]");
            foreach (MyEntity target in entList)
            {
                if (grinder.OwnerIdentityId == 0) break;
                Grid grid = _mediator.GridCollectionController.GetGrid(target.EntityId);
                WriteGeneral(nameof(RunGrinderLogic), $"Looking for: [{(grid == null ? "T" : "F")}] [{target.EntityId:D18}] [{grinder.OwnerIdentityId:D18}]");
                if (grid == null || grid.GridOwnershipController.OwnershipType != OwnershipType.Npc) continue;
                WriteGeneral(nameof(RunGrinderLogic), $"Found: [{target.EntityId:D18}]");
                grid.TriggerHighlights(grinder.OwnerIdentityId);
            }
        }
    }
}