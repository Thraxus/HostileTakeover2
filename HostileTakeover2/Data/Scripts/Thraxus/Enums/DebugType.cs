using HostileTakeover2.Thraxus.Common.BaseClasses;

namespace HostileTakeover2.Thraxus.Enums
{
    /// <summary>
    /// Enumeration Class defining the subsystem-level debug categories for this mod.
    /// Extends <see cref="LogCategory"/> (defined in Common) so that the logging
    /// infrastructure remains portable while each mod provides its own category set.
    ///
    /// Set <see cref="Utility.UserConfig.Models.DefaultSettings.ActiveDebugCategories"/>
    /// in the config file to a comma-separated list of category names (e.g. "Grid, Highlight")
    /// to limit log output to those systems when DebugMode is active.
    /// VerboseMode always enables all categories regardless of this setting.
    ///
    /// Call <see cref="Initialize"/> from <c>EarlyInit()</c> in the session component
    /// to guarantee all singletons are registered before any settings parsing or
    /// log-gating calls occur.
    /// </summary>
    public sealed class DebugType : LogCategory
    {
        public static readonly DebugType None      = new DebugType(0,      "None");
        public static readonly DebugType Grid      = new DebugType(1 << 0, "Grid");
        public static readonly DebugType Ownership = new DebugType(1 << 1, "Ownership");
        public static readonly DebugType Blocks    = new DebugType(1 << 2, "Blocks");
        public static readonly DebugType Highlight = new DebugType(1 << 3, "Highlight");
        public static readonly DebugType Grinder   = new DebugType(1 << 4, "Grinder");
        public static readonly DebugType Pool      = new DebugType(1 << 5, "Pool");
        public static readonly DebugType Construct = new DebugType(1 << 6, "Construct");

        private DebugType(int id, string name) : base(id, name) { }

        /// <summary>
        /// Explicit static constructor removes the <c>beforefieldinit</c> IL flag,
        /// giving this class precise initialization semantics.  Without it the runtime
        /// is only required to run static field initializers before the first static
        /// <em>field</em> access, not before a static method call — so
        /// <see cref="Initialize"/> (an empty method) could be called without the
        /// singletons ever being constructed, leaving the <see cref="LogCategory"/>
        /// registry empty.  With this constructor present, any static member access
        /// (including the <see cref="Initialize"/> call from <c>EarlyInit</c>)
        /// is guaranteed to trigger all field initializers first.
        /// </summary>
        // ReSharper disable once EmptyConstructor
        static DebugType() { }

        /// <summary>
        /// Forces static initialization of all <see cref="DebugType"/> singletons,
        /// populating the <see cref="LogCategory"/> registry before settings are parsed.
        /// Call this from <c>EarlyInit()</c> in the session component.
        /// </summary>
        public static void Initialize() { }
    }
}
