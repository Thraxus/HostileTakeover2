using System;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Common
{
    /// <summary>
    /// Module-wide read-only reference values that are fixed for the lifetime of the
    /// session.  Not intended to be user-configurable.
    /// </summary>
    public static class References
    {   // These settings should be used by the mod directly, and not changeable by a user.  They are considered "reference only"

        //public const string ChatCommandPrefix = "chatCommand";
        //public const string ExceptionLogName = "Exception";
        //public const ushort NetworkId = 16759;

        #region Reference Values

        /// <summary>
        /// True when the current machine is acting as the authoritative simulation host
        /// (dedicated server or host-client in single player / listen server).
        /// </summary>
        public static bool IsServer => MyAPIGateway.Multiplayer.IsServer;

        /// <summary>Default duration (milliseconds) for HUD notifications shown to the local player.</summary>
        public const int DefaultLocalMessageDisplayTime = 5000;
        /// <summary>Default duration (milliseconds) for server-side HUD notifications.</summary>
        public const int DefaultServerMessageDisplayTime = 10000;

        /// <summary>Number of simulation ticks per real-world second (Space Engineers runs at 60 Hz).</summary>
        public const int TicksPerSecond = 60;
        /// <summary>Number of simulation ticks per real-world minute (60 seconds × 60 ticks/sec).</summary>
        public const int TicksPerMinute = TicksPerSecond * 60;

        /// <summary>Shared random number generator for the mod; avoids per-site instantiation.</summary>
        public static Random Random { get; } = new Random();

        #endregion
    }
}
