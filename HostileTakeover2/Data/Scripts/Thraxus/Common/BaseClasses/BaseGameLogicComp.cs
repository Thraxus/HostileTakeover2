using VRage.Game.Components;

namespace HostileTakeover2.Thraxus.Common.BaseClasses
{
    /// <summary>
    /// Abstract base class for game-logic components (block-level components that attach
    /// to individual entities).  Extends <see cref="MyGameLogicComponent"/> with a simple
    /// tick counter and an abstract timer hook so subclasses only need to implement their
    /// periodic logic without boilerplate.
    /// </summary>
    internal abstract class BaseGameLogicComp : MyGameLogicComponent
    {
        /// <summary>Display name of the entity this component is attached to.</summary>
        protected string EntityName = "PlaceholderName";
        /// <summary>Entity ID of the owning entity.</summary>
        protected long EntityId = 0L;

        /// <summary>
        /// Monotonically increasing counter incremented once per simulation tick.
        /// Subclasses use this to implement interval-based logic (e.g. every N ticks).
        /// </summary>
        protected long Ticks;

        /// <summary>
        /// Increments <see cref="Ticks"/> and calls the abstract <see cref="TickTimer"/>
        /// once per simulation tick before delegating to the base implementation.
        /// </summary>
        public override void UpdateBeforeSimulation()
        {
            Ticks++;
            TickTimer();
            base.UpdateBeforeSimulation();
        }

        /// <summary>
        /// Subclass hook called every tick after <see cref="Ticks"/> has been incremented.
        /// Implement interval checks here (e.g. <c>if (Ticks % 60 == 0) { ... }</c>).
        /// </summary>
        protected abstract void TickTimer();
    }
}
