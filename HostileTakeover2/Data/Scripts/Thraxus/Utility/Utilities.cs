using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Models;

namespace HostileTakeover2.Thraxus.Utility
{
    internal class Utilities
    {
        public readonly ActionQueue ActionQueue = new ActionQueue();

        public readonly ObjectPool<Grid> GridPool = new ObjectPool<Grid>(() => new Grid());
        public readonly ObjectPool<Block> BlockPool = new ObjectPool<Block>(() => new Block());

        public readonly GridController GridController = new GridController();
        public readonly GrinderController GrinderController = new GrinderController();
        public readonly HighlightController HighlightController = new HighlightController();

        public Utilities()
        {
            GrinderController.Init(this);
            HighlightController.Init(this);
        }
    }
}