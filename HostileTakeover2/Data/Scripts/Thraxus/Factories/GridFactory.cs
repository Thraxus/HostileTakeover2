using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Enums;

namespace HostileTakeover2.Thraxus.Factories
{
    internal class GridFactory
    {
        private readonly ObjectPool<NpcGrid> _npcGridObjectPool = new ObjectPool<NpcGrid>();
        private readonly ObjectPool<UnownedGrid> _unownedGridObjectPool = new ObjectPool<UnownedGrid>();
        private readonly ObjectPool<PlayerGrid> _playerGridObjectPool = new ObjectPool<PlayerGrid>();

        public BaseGrid GetGrid(OwnerType type)
        {
            switch (type)
            {
                case OwnerType.None:
                    return _unownedGridObjectPool.Get();
                case OwnerType.Npc:
                    return _npcGridObjectPool.Get();
                case OwnerType.Player:
                    return _playerGridObjectPool.Get();
                case OwnerType.NotEvaluated:
                case OwnerType.UnderReview:
                default:
                    break;
            }
            return null;
        }

        public void ReturnGrid(BaseGrid grid)
        {
            var npcGrid = grid as NpcGrid;
            if (npcGrid != null)
            {
                _npcGridObjectPool.Return(npcGrid);
                return;
            }

            var playerGrid = grid as PlayerGrid;
            if (playerGrid != null)
            {
                _playerGridObjectPool.Return(playerGrid);
                return;
            }

            _unownedGridObjectPool.Return((UnownedGrid)grid);
        }

        public override string ToString()
        {
            return $"\n\t\t\t{_npcGridObjectPool}\n\t\t\t{_playerGridObjectPool}\n\t\t\t{_unownedGridObjectPool}";
        }
    }
}