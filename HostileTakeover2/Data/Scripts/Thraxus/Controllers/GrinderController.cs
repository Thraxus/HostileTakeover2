using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Infrastructure;
using HostileTakeover2.Thraxus.Models;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GrinderController : BaseLoggingClass, IInit<Mediator>
    {
        private Mediator _mediator;

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }

        private readonly List<MyEntity> _reusableEntityList = new List<MyEntity>();
        private readonly HashSet<IMyGridGroupData> _seenGroupData = new HashSet<IMyGridGroupData>();

        private void GrabAllNearbyGrids(Vector3D center)
        {
            _reusableEntityList.Clear();
            // Use the configured detection range from user settings.
            var pruneSphere = new BoundingSphereD(center, _mediator.DefaultSettings.EntityDetectionRange.Current);
            MyGamePruningStructure.GetAllEntitiesInSphere(ref pruneSphere, _reusableEntityList);
            // Remove any non-grid entities (characters, floating objects, etc.) in reverse order
            // so RemoveAtFast can swap-and-shrink without corrupting the remaining indices.
            for (int i = _reusableEntityList.Count - 1; i >= 0; i--)
            {
                if (!(_reusableEntityList[i] is MyCubeGrid))
                    _reusableEntityList.RemoveAtFast(i);
            }
        }

        private Construct FilterToNearestConstruct(Vector3D source)
        {
            double distance = double.MaxValue;
            Construct closestConstruct = null;
            foreach (var entity in _reusableEntityList)
            {
                // Resolve the topmost parent so that subgrids (rotor heads, piston heads)
                // map to the Construct registered under the parent's EntityId.
                var topMost = entity.GetTopMostParent() as MyCubeGrid;
                Construct construct = _mediator.ConstructController.GetConstruct(topMost?.EntityId ?? entity.EntityId);
                // Skip untracked entities and non-NPC grids.
                if (construct == null || construct.GridOwnershipController.OwnershipType != OwnershipType.Npc) continue;

                // Transform source into the grid's local space, clamp to the local AABB
                // (grid-axis-aligned, so it hugs the actual block geometry), then bring
                // the nearest point back into world space for the distance measurement.
                MatrixD worldMatrixInv = MatrixD.Invert(entity.WorldMatrix);
                Vector3D localSource = Vector3D.Transform(source, worldMatrixInv);
                BoundingBoxD localAabb = entity.PositionComp.LocalAABB;
                Vector3D nearestLocal = Vector3D.Clamp(localSource, localAabb.Min, localAabb.Max);
                Vector3D nearestWorld = Vector3D.Transform(nearestLocal, entity.WorldMatrix);
                double abs = (nearestWorld - source).LengthSquared();

                if (_mediator.DefaultSettings.IsVerboseActiveFor(DebugType.Grinder))
                    WriteGeneral(DebugType.Grinder, nameof(FilterToNearestConstruct), $"Validating possible grid as target: [{(abs > distance).ToSingleChar()}] [{abs:##.###}] [{distance:E3}] [{construct.EntityId:D18}]");
                if (abs > distance) continue;
                distance = abs;
                closestConstruct = construct;
            }
            return closestConstruct;
        }

        public void RunGrinderLogic(IMyAngleGrinder grinder)
        {
            if (grinder.OwnerIdentityId == 0) return;
            GrabAllNearbyGrids(grinder.GetPosition());
            var construct = FilterToNearestConstruct(grinder.GetPosition());
            if (construct == null)
            {
                WriteGeneral(nameof(RunGrinderLogic), $"No NPC grid found near grinder [{grinder.EntityId:D18}]");
                return;
            }
            WriteGeneral(nameof(RunGrinderLogic), $"Found: [{construct.EntityId:D18}]");
            if (_mediator.DefaultSettings.HighlightAllGridsInRange.Current)
                TriggerHighlightsForAllNearbyNpcGrids(grinder.OwnerIdentityId);
            else
                construct.TriggerHighlights(grinder.OwnerIdentityId);

            if (_mediator.DefaultSettings.IsDebugActive)
            {
                long playerId = grinder.OwnerIdentityId;
                DebugVisualizeDetection(grinder.GetPosition(), playerId, construct.EntityId);
                long capturedPlayerId = playerId;
                grinder.OnMarkForClose += entity => ClearDebugMarkersForPlayer(capturedPlayerId);
            }

        }

        private void TriggerHighlightsForAllNearbyNpcGrids(long grinderOwnerIdentityId)
        {
            if (!_mediator.DefaultSettings.UseHighlights.Current) return;
            _seenGroupData.Clear();
            foreach (var entity in _reusableEntityList)
            {
                var topMost = entity.GetTopMostParent() as MyCubeGrid;
                Construct npcConstruct = _mediator.ConstructController.GetConstruct(topMost?.EntityId ?? entity.EntityId);
                if (npcConstruct == null || npcConstruct.GridOwnershipController.OwnershipType != OwnershipType.Npc) continue;
                var groupData = npcConstruct.GridGroupManager.GridGroupData;
                if (groupData == null || !_seenGroupData.Add(groupData)) continue;
                _mediator.HighlightController.EnableHighlights(groupData, grinderOwnerIdentityId);
            }
        }

        // ── Debug utilities ──────────────────────────────────────────────────────────
        // Gated on DefaultSettings.DebugMode; set DebugMode = false in the settings file
        // before publishing the mod to suppress all GPS marker output.

        private readonly Dictionary<long, List<IMyGps>> _debugGpsMarkers = new Dictionary<long, List<IMyGps>>();

        /// <summary>
        /// Places GPS markers in the world to visualise the state of the detection sphere
        /// immediately after <see cref="GrabAllNearbyGrids"/> populates
        /// <c>_reusableEntityList</c>.  Call this once per grinder-equip event.
        ///
        /// Marker colour / meaning (always visible at DebugMode level unless noted):
        /// <list type="bullet">
        ///   <item><b>White — GRINDER</b>: detection sphere centre (grinder world position).</item>
        ///   <item><b>DeepSkyBlue — [NPC:N]_NEAR</b>: nearest OBB point for the selected
        ///     (closest) NPC grid — the one that receives highlights and the log dump.</item>
        ///   <item><b>LimeGreen — [NPC:N]_NEAR</b>: nearest OBB point for all other detected
        ///     NPC grids.  Distance label shows how far they lost by.</item>
        ///   <item><b>OrangeRed — [NPC:N]_cX</b>: <i>(VerboseMode only)</i> the 8 world-AABB
        ///     corners of each NPC grid found in the sphere.</item>
        ///   <item><b>LightGray — [UNK/TRK:N]_cX</b>: <i>(VerboseMode only)</i> same for
        ///     non-NPC or untracked grids.</item>
        ///   <item><b>Yellow — [UNK/TRK:N]_NEAR</b>: <i>(VerboseMode only)</i> nearest-point
        ///     marker for non-NPC grids.</item>
        /// </list>
        ///
        /// Note: <c>MyAPIGateway.Session.GPS</c> is a client-side API.  In single-player
        /// the server and client share the same process so this works without network
        /// messages.  Do not use in a dedicated-server context without adding a packet.
        /// </summary>
        /// <summary>
        /// Removes all GPS debug markers for <paramref name="playerId"/> from the HUD
        /// and clears the tracking list.  Safe to call when no markers exist.
        /// </summary>
        private void ClearDebugMarkersForPlayer(long playerId)
        {
            List<IMyGps> markers;
            if (!_debugGpsMarkers.TryGetValue(playerId, out markers)) return;
            foreach (var gps in markers)
                MyAPIGateway.Session.GPS.RemoveLocalGps(gps);
            markers.Clear();
        }

        private void DebugVisualizeDetection(Vector3D grinderPos, long playerId, long selectedEntityId)
        {
            // Remove markers placed by the previous equip for this player.
            ClearDebugMarkersForPlayer(playerId);
            List<IMyGps> markers;
            if (!_debugGpsMarkers.TryGetValue(playerId, out markers))
            {
                markers = new List<IMyGps>();
                _debugGpsMarkers[playerId] = markers;
            }

            // Detection sphere centre.
            AddDebugGps(markers, ">>GRINDER<<", grinderPos, Color.White, playerId);

            int index = 0;
            foreach (var entity in _reusableEntityList)
            {
                var topMost = entity.GetTopMostParent() as MyCubeGrid;
                var trackedConstruct = _mediator.ConstructController.GetConstruct(topMost?.EntityId ?? entity.EntityId);
                bool isNpc = trackedConstruct != null &&
                             trackedConstruct.GridOwnershipController.OwnershipType == OwnershipType.Npc;
                string tag = isNpc ? "NPC" : (trackedConstruct != null ? "TRK" : "UNK");

                // Verbose-only: world AABB corners (8 per grid is too noisy at debug level).
                if (_mediator.DefaultSettings.IsVerboseActiveFor(DebugType.Grinder))
                {
                    Color cornerColor = isNpc ? Color.OrangeRed : Color.LightGray;
                    var aabb = entity.PositionComp.WorldAABB;
                    Vector3D lo = aabb.Min, hi = aabb.Max;
                    AddDebugGps(markers, $"[{tag}:{index}]_c0", new Vector3D(lo.X, lo.Y, lo.Z), cornerColor, playerId);
                    AddDebugGps(markers, $"[{tag}:{index}]_c1", new Vector3D(hi.X, lo.Y, lo.Z), cornerColor, playerId);
                    AddDebugGps(markers, $"[{tag}:{index}]_c2", new Vector3D(lo.X, hi.Y, lo.Z), cornerColor, playerId);
                    AddDebugGps(markers, $"[{tag}:{index}]_c3", new Vector3D(hi.X, hi.Y, lo.Z), cornerColor, playerId);
                    AddDebugGps(markers, $"[{tag}:{index}]_c4", new Vector3D(lo.X, lo.Y, hi.Z), cornerColor, playerId);
                    AddDebugGps(markers, $"[{tag}:{index}]_c5", new Vector3D(hi.X, lo.Y, hi.Z), cornerColor, playerId);
                    AddDebugGps(markers, $"[{tag}:{index}]_c6", new Vector3D(lo.X, hi.Y, hi.Z), cornerColor, playerId);
                    AddDebugGps(markers, $"[{tag}:{index}]_c7", new Vector3D(hi.X, hi.Y, hi.Z), cornerColor, playerId);
                }

                // Nearest point on the OBB — mirrors FilterToNearestGrid exactly.
                // Transform grinder into grid-local space, clamp to local AABB, transform back.
                MatrixD worldMatrixInv = MatrixD.Invert(entity.WorldMatrix);
                Vector3D localGrinder = Vector3D.Transform(grinderPos, worldMatrixInv);
                BoundingBoxD localAabb = entity.PositionComp.LocalAABB;
                Vector3D nearestLocal = Vector3D.Clamp(localGrinder, localAabb.Min, localAabb.Max);
                Vector3D nearestWorld = Vector3D.Transform(nearestLocal, entity.WorldMatrix);
                double dist = (nearestWorld - grinderPos).Length();
                // NPC nearest point: always shown at debug level.
                //   Selected (target) grid → DeepSkyBlue; all other NPC grids → LimeGreen.
                // Non-NPC nearest point: verbose-only (Yellow).
                if (isNpc)
                {
                    Color nearestColor = entity.EntityId == selectedEntityId ? Color.DeepSkyBlue : Color.LimeGreen;
                    AddDebugGps(markers, $"[{tag}:{index}]_NEAR_{dist:F1}m", nearestWorld, nearestColor, playerId);
                }
                else if (_mediator.DefaultSettings.IsVerboseActiveFor(DebugType.Grinder))
                    AddDebugGps(markers, $"[{tag}:{index}]_NEAR_{dist:F1}m", nearestWorld, Color.Yellow, playerId);

                index++;
            }
        }

        /// <summary>
        /// Creates a temporary GPS waypoint, registers it with the game for
        /// <paramref name="playerId"/>, and adds it to <paramref name="markers"/> so it
        /// can be cleaned up on the next call to <see cref="DebugVisualizeDetection"/>.
        /// </summary>
        private static void AddDebugGps(List<IMyGps> markers, string name, Vector3D position, Color color, long playerId)
        {
            var gps = MyAPIGateway.Session.GPS.Create(name, name, position, showOnHud: true, temporary: true);
            gps.GPSColor = color;
            MyAPIGateway.Session.GPS.AddLocalGps(gps);
            markers.Add(gps);
        }

    }
}