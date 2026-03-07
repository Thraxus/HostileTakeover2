using System;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class GridGroupManager : BaseLoggingClass
    {
        private IMyGridGroupData _gridGroupData;
        private MyCubeGrid _me;

        public IMyGridGroupData GridGroupData => _gridGroupData;

        public Action<IMyCubeGrid> GridAddedAction;
        public Action<IMyCubeGrid, IMyGridGroupData> GridRemovedAction;

        public void Init(MyCubeGrid me)
        {
            _me = me;
            Refresh();
        }

        public void Refresh()
        {
            Refresh(_me.GetGridGroup(GridLinkTypeEnum.Logical));
        }

        public void Refresh(IMyGridGroupData newGroup)
        {
            WriteGeneral(nameof(Refresh), $"Refreshing grid group data for [{_me.EntityId.ToEntityIdFormat()}].");
            if (_gridGroupData != null) DeRegister();
            _gridGroupData = newGroup;
            if (_gridGroupData == null)
            {
                WriteGeneral(nameof(Refresh), $"Grid group data was null for [{_me.EntityId.ToEntityIdFormat()}].");
                return;
            }
            Register();
        }

        private void Register()
        {
            _gridGroupData.OnGridAdded += OnGridAdded;
            _gridGroupData.OnGridRemoved += OnGridRemoved;
            _gridGroupData.OnReleased += OnReleased;
        }

        private void DeRegister()
        {
            _gridGroupData.OnReleased -= OnReleased;
            _gridGroupData.OnGridAdded -= OnGridAdded;
            _gridGroupData.OnGridRemoved -= OnGridRemoved;
        }

        private void OnReleased(IMyGridGroupData data)
        {
            try
            {
                DeRegister();
                _gridGroupData = null;
            }
            catch (Exception e) { WriteGeneral(nameof(OnReleased), $"Exception: {e}"); }
        }

        private void OnGridAdded(IMyGridGroupData newGroup, IMyCubeGrid addedGrid, IMyGridGroupData oldGroup)
        {
            try { GridAddedAction?.Invoke(addedGrid); }
            catch (Exception e) { WriteGeneral(nameof(OnGridAdded), $"Exception: {e}"); }
        }

        private void OnGridRemoved(IMyGridGroupData thisGroup, IMyCubeGrid removedGrid, IMyGridGroupData newGroup)
        {
            try { GridRemovedAction?.Invoke(removedGrid, newGroup); }
            catch (Exception e) { WriteGeneral(nameof(OnGridRemoved), $"Exception: {e}"); }
        }

        public override void Reset()
        {
            if (_gridGroupData != null) DeRegister();
            _gridGroupData = null;
            GridAddedAction = null;
            GridRemovedAction = null;
            _me = null;
            base.Reset();
        }
    }
}
