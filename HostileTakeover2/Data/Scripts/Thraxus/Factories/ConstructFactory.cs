using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Generics;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Controllers;
using Sandbox.Game.Entities;

namespace HostileTakeover2.Thraxus.Factories
{
    internal class ConstructFactory : BaseLoggingClass, IActionQueue
    {
        internal readonly ObjectPool<ConstructController> ConstructPool;
        public ActionQueue ActionQueue { get; set; }

        private static int _constructCounter;

        public ConstructFactory(ActionQueue actionQueue)
        {
            ActionQueue = actionQueue;
            ConstructPool = new ObjectPool<ConstructController>(() => new ConstructController());
        }
        
        public ConstructController SetupNewConstruct(MyCubeGrid grid)
        {
            var newConstruct = ConstructPool.Get();
            newConstruct.Initialize(++_constructCounter, ActionQueue);
            newConstruct.AddGrids(grid);
            return newConstruct;
        }

        public void ReturnConstruct(ConstructController construct)
        {
            ConstructPool.Return(construct);
        }
    }
}