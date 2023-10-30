using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    public class GridGroupController : BaseLoggingClass
    {
        private readonly Dictionary<IMyGridGroupData, ReusableHashset<IMyCubeGrid>> _groupGridMap = new Dictionary<IMyGridGroupData, ReusableHashset<IMyCubeGrid>>();
        private readonly Mediator _mediator;
        
        public GridGroupController(Mediator mediator)
        {
            _mediator = mediator;
        }

        public ReusableHashset<IMyCubeGrid> GetGridGroup(IMyGridGroupData myGridGroupData)
        {
            return _groupGridMap[myGridGroupData];
        }

        public void Init()
        {
            MyAPIGateway.GridGroups.OnGridGroupCreated += GroupCreated;
            MyAPIGateway.GridGroups.OnGridGroupDestroyed += GroupDestroyed;
            var x = new HashSet<IMyGridGroupData>();
            MyAPIGateway.GridGroups.GetGridGroups(GridLinkTypeEnum.Logical, x);
            foreach (var gridGroup in x)
            {
                GroupCreated(gridGroup);
            }
        }

        private void GroupCreated(IMyGridGroupData myGridGroupData)
        {
            myGridGroupData.OnGridAdded += OnGridAdded;
            myGridGroupData.OnGridRemoved += OnGridRemoved;
            myGridGroupData.OnReleased += GroupDestroyed;
            ReusableHashset<IMyCubeGrid> reusableMyCubeGridList = _mediator.GetReusableMyCubeGridList(myGridGroupData);
            _groupGridMap.Add(myGridGroupData, reusableMyCubeGridList);
        }

        private void GroupDestroyed(IMyGridGroupData myGridGroupData)
        {
            if (!_groupGridMap.ContainsKey(myGridGroupData)) return;
            ClearGridGroupSubscriptions(myGridGroupData);
            _mediator.ReturnReusableMyCubeGridList(_groupGridMap[myGridGroupData]);
            _groupGridMap.Remove(myGridGroupData);
        }

        private void ClearGridGroupSubscriptions(IMyGridGroupData myGridGroupData)
        {
            //WriteGeneral(nameof(ClearGridGroupSubscriptions), $"Clearing GridGroupData");
            myGridGroupData.OnGridAdded -= OnGridAdded;
            myGridGroupData.OnGridRemoved -= OnGridRemoved;
            myGridGroupData.OnReleased -= GroupDestroyed;
        }

        private void ReturnReusableCollections()
        {
            //WriteGeneral(nameof(ReturnReusableCollections), $"Returning Collections");
            foreach (var map in _groupGridMap)
            {
                _mediator.ReturnReusableMyCubeGridList(map.Value);
            }
        }

        private void OnGridAdded(IMyGridGroupData thisGridGroup, IMyCubeGrid grid, IMyGridGroupData oldGridGroup)
        {
            WriteGeneral(nameof(OnGridAdded), $"Grid added: [{grid.EntityId.ToEntityIdFormat()}]" );
            _groupGridMap[thisGridGroup].Add(grid);
        }

        private void OnGridRemoved(IMyGridGroupData thisGridGroup, IMyCubeGrid grid, IMyGridGroupData newGridGroup)
        {
            WriteGeneral(nameof(OnGridRemoved), $"Grid removed: [{grid.EntityId.ToEntityIdFormat()}]");
            //Todo This will need a kick to reevaluate the grid.  it's really the only reason any of this shit exists. 
            _groupGridMap[thisGridGroup].Remove(grid);
        }

        public override void Close()
        {
            //WriteGeneral(nameof(Close), $"Closing GridGroupController");
            base.Close();
            //WriteGeneral(nameof(Close), $"base.Close() clear");

            // TODO Figured out if the below is needed.  At current, they cause a threaded / hidden crash if called from Session.UnloadData
            //MyAPIGateway.GridGroups.OnGridGroupCreated -= GroupCreated;
            //WriteGeneral(nameof(Close), $"Unsubscribed from OnGridGroupCreated");
            //MyAPIGateway.GridGroups.OnGridGroupDestroyed -= GroupDestroyed;
            //WriteGeneral(nameof(Close), $"Unsubscribed from OnGridGroupDestroyed");

            foreach (var map in _groupGridMap)
                ClearGridGroupSubscriptions(map.Key);
            //WriteGeneral(nameof(Close), $"GridGroupSubscriptions clear");
            
            ReturnReusableCollections();
            //WriteGeneral(nameof(Close), $"Reusable Collections returned");
            
            _groupGridMap.Clear();
            //WriteGeneral(nameof(Close), $"Collection emptied");
        }
    }
}
