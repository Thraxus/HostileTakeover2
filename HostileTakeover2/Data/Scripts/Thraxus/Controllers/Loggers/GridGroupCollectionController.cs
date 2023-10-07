using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Models;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridGroupCollectionController : BaseLoggingClass
    {
        private readonly ObjectPool<ReusableCubeGridList<IMyCubeGrid>> _reusableMyCubeGridCollectionObjectPool =
            new ObjectPool<ReusableCubeGridList<IMyCubeGrid>>(() => new ReusableCubeGridList<IMyCubeGrid>());

        public ReusableCubeGridList<IMyCubeGrid> Get(IMyGridGroupData myGridGroupData)
        {
            WriteGeneral(nameof(GridGroupCollectionController), $"Get -- Lending a ReusableCubeGridList [{_reusableMyCubeGridCollectionObjectPool.Count():D3}]");
            var list = _reusableMyCubeGridCollectionObjectPool.Get();
            myGridGroupData.GetGrids(list);
            return list;
        }

        public void Return(ReusableCubeGridList<IMyCubeGrid> list)
        {
            WriteGeneral(nameof(GridGroupCollectionController), $"Return -- Returning a ReusableCubeGridList [{_reusableMyCubeGridCollectionObjectPool.Count():D3}]");
            _reusableMyCubeGridCollectionObjectPool.Return(list);
        }
    }
}