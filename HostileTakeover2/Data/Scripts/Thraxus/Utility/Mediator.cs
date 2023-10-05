using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Models;

namespace HostileTakeover2.Thraxus.Utility
{
    internal class Mediator
    {
        public readonly ActionQueue ActionQueue = new ActionQueue();

        public readonly ObjectPool<Grid> GridPool = new ObjectPool<Grid>(() => new Grid());
        public readonly ObjectPool<Block> BlockPool = new ObjectPool<Block>(() => new Block());
        public readonly ObjectPool<HighlightSettings> HighlightSettingsPool = new ObjectPool<HighlightSettings>(() => new HighlightSettings());

        public readonly GridGroupCollectionController GridGroupCollectionController = new GridGroupCollectionController();
        public readonly GridGroupCoordinationController GridGroupCoordinationController = new GridGroupCoordinationController();
        public readonly GrinderController GrinderController = new GrinderController();
        public readonly HighlightController HighlightController = new HighlightController();

        public Mediator()
        {
            GridGroupCoordinationController.Init(this);
            GrinderController.Init(this);
            HighlightController.Init(this);
        }
    }
}