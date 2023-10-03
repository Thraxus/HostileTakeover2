using Sandbox.Game.Entities;
using Sandbox.ModAPI.Weapons;
using Sandbox.ModAPI;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;
using DefaultSettings = HostileTakeover2.Thraxus.Utility.UserConfig.Settings.DefaultSettings;
using HostileTakeover2.Thraxus.Utility;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GrinderController : BaseLoggingClass
    {
        private Utilities _utilities;

        public void Init(Utilities utilities)
        {
            _utilities = utilities;
        }

        private readonly List<MyEntity> _reusableEntityList = new List<MyEntity>();

        private List<MyEntity> GrabNearbyGrids(Vector3D center)
        {
            _reusableEntityList.Clear();
            var pruneSphere = new BoundingSphereD(center, DefaultSettings.EntityDetectionRange.Current);
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
            WriteGeneral(nameof(RunGrinderLogic), $"Grinder: [{grinder.OwnerIdentityId:D20}] [{grinder.OwnerId:D20}] [{entList.Count:D2}] [{grinder.GetPosition()}] [{entityById?.GetPosition()}]");
            foreach (MyEntity target in entList)
            {
                if (grinder.OwnerIdentityId == 0) break;
                Grid grid = _utilities.GridController.GetGrid(target.EntityId);
                WriteGeneral(nameof(RunGrinderLogic), $"Looking for: [{(grid == null ? "T" : "F")}] [{target.EntityId:D20}] [{grinder.OwnerIdentityId:D20}]");
                if (grid == null || grid.Ownership != OwnershipType.Npc) continue;
                WriteGeneral(nameof(RunGrinderLogic), $"Found: [{target.EntityId:D20}]");
                grid.TriggerHighlights();
            }
        }
    }
}