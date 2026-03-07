using System.Text;
using HostileTakeover2.Thraxus.Common.Enums;
using HostileTakeover2.Thraxus.Common.Utilities.Tools.Logging;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace HostileTakeover2.Thraxus.Common.BaseClasses
{
    /// <summary>
    /// Base class for session components in this mod.  Wraps the Space Engineers
    /// <see cref="MySessionComponentBase"/> lifecycle with:
    /// <list type="bullet">
    ///   <item>A server/client gate (<see cref="BlockUpdates"/>) so code never runs on
    ///     the wrong side of the network.</item>
    ///   <item>A three-phase setup sequence (SuperEarlySetup / EarlySetup / LateSetup)
    ///     that maps onto the LoadData → Init → UpdateBeforeSimulation lifecycle.</item>
    ///   <item>Integrated mod-local file logging via <see cref="Log"/>.</item>
    ///   <item>A startup diagnostic dump (<see cref="BasicInformationDump"/>) that logs
    ///     game settings, installed mods, factions, and stored identities.</item>
    /// </list>
    /// </summary>
    public abstract class BaseSessionComp : MySessionComponentBase
    {
        /// <summary>Short name used as the log file prefix and in log lines.</summary>
        protected abstract string CompName { get; }

        /// <summary>
        /// Controls which network side this component runs on.
        /// <see cref="CompType.Server"/> → server only,
        /// <see cref="CompType.Client"/> → client only,
        /// <see cref="CompType.Both"/> → always active.
        /// </summary>
        protected abstract CompType Type { get; }

        /// <summary>
        /// The desired <see cref="MyUpdateOrder"/> for this component once fully
        /// initialised.  Applied during <see cref="LateSetup"/> via
        /// <c>InvokeOnGameThread</c>.
        /// </summary>
        protected abstract MyUpdateOrder Schedule { get; }

        private Log _generalLog;

        private bool _superEarlySetupComplete;
        private bool _earlySetupComplete;
        private bool _lateSetupComplete;

        /// <summary>
        /// Returns <c>true</c> when this component should NOT execute on the current
        /// machine (i.e. a server-only component running on a client, or vice versa).
        /// Used as a guard at the top of every lifecycle method so that update callbacks
        /// registered with the game don't run unwanted code on the wrong side.
        /// </summary>
        private bool BlockUpdates()
        {
            switch (Type)
            {
                case CompType.Both:
                    return false;
                case CompType.Client:
                    // Block if running on the server (client-only component).
                    return References.IsServer;
                case CompType.Server:
                    // Block if NOT running on the server (server-only component).
                    return !References.IsServer;
                default:
                    return false;
            }
        }

        /// <summary>
        ///  Amongst the earliest execution points, but not everything is available at this point.
        ///  Main entry point: MyAPIGateway
        ///  Entry point for reading/editing definitions: MyDefinitionManager.Static
        /// </summary>
        public override void LoadData()
        {
            if (BlockUpdates())
            {
                // Wrong network side: immediately zero-out the update schedule so this
                // component never receives further update callbacks.
                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate)); // sets the proper update schedule to the desired schedule
                return;
            };
            base.LoadData();
            EarlyInit();
            if (!_superEarlySetupComplete) SuperEarlySetup();
        }

        /// <summary>
        /// Hook called at the very start of <see cref="LoadData"/> (before SuperEarlySetup),
        /// guaranteed to run before any settings parsing or event subscriptions.
        /// Override in subclasses to perform type-initialization tasks such as populating
        /// Enumeration Class registries (e.g. <c>DebugType.Initialize()</c>).
        /// </summary>
        protected virtual void EarlyInit() { }

        /// <summary>
        ///  Always return base.GetObjectBuilder(); after your code!
        ///  Do all saving here, make sure to return the OB when done;
        /// </summary>
        /// <returns> Object builder for the session component </returns>
        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            return base.GetObjectBuilder();
        }

        /// <summary>
        ///  This save happens after the game save, so it has limited uses really
        /// </summary>
        public override void SaveData()
        {

            base.SaveData();
        }

        /// <summary>
        /// Phase 1 setup: called from <see cref="LoadData"/> the first time it runs.
        /// Creates the log file and records the server/client state.
        /// Subclasses should call <c>base.SuperEarlySetup()</c> first.
        /// </summary>
        protected virtual void SuperEarlySetup()
        {
            _superEarlySetupComplete = true;
            _generalLog = new Log(CompName);
            WriteGeneral("SuperEarlySetup", $"Waking up.  Is Server: {References.IsServer}");
        }

        /// <summary>
        ///  Executed before the world starts updating
        /// </summary>
        public override void BeforeStart()
        {
            if (BlockUpdates()) return;
            base.BeforeStart();
            // Emit a one-time diagnostic snapshot to the log so server admins can verify
            // the mod loaded correctly and see the game/mod/faction state at startup.
            BasicInformationDump();
        }

        /// <summary>
        /// Collects and logs game settings, installed mods, faction list, and stored
        /// identity list into a single block at startup.  Useful for post-hoc debugging.
        /// </summary>
        private void BasicInformationDump()
        {
            var sb = new StringBuilder();
            Reporting.GameSettings.Report(sb);
            Reporting.InstalledMods.Report(sb);
            Reporting.ExistingFactions.Report(sb);
            Reporting.StoredIdentities.Report(sb);
            WriteGeneral(sb.ToString());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sessionComponent"></param>
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (BlockUpdates()) return;
            base.Init(sessionComponent);
            if (!_earlySetupComplete) EarlySetup();
        }

        /// <summary>
        /// Phase 2 setup: called from <see cref="Init"/> the first time it runs.
        /// Available game state is broader than SuperEarlySetup but the world is not yet
        /// simulating.
        /// </summary>
        protected virtual void EarlySetup()
        {
            _earlySetupComplete = true;
        }

        /// <summary>
        ///  Executed every tick, 60 times a second, before physics simulation and only if game is not paused.
        /// </summary>
        public override void UpdateBeforeSimulation()
        {
            if (BlockUpdates()) return;
            base.UpdateBeforeSimulation();
            if (!_lateSetupComplete) LateSetup();
            UpdateBeforeSim();
        }

        /// <summary>Virtual hook called every tick in UpdateBeforeSimulation after setup is complete.</summary>
        protected virtual void UpdateBeforeSim() { }

        /// <summary>
        /// Phase 3 setup: called the first time <see cref="UpdateBeforeSimulation"/> runs
        /// (i.e. after the first simulation tick).  Uses <c>InvokeOnGameThread</c> to
        /// apply the final desired <see cref="Schedule"/> because <c>SetUpdateOrder</c>
        /// must be called from the game thread to take effect safely.
        /// </summary>
        protected virtual void LateSetup()
        {
            _lateSetupComplete = true;
            // Only change the update order if it differs from the current one to avoid
            // an unnecessary engine call.
            if (UpdateOrder != Schedule)
                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(Schedule)); // sets the proper update schedule to the desired schedule
            WriteGeneral("LateSetup", $"Fully online.");
        }

        /// <summary>
        ///  Executed every tick, 60 times a second, after physics simulation and only if game is not paused.
        /// </summary>
        public override void UpdateAfterSimulation()
        {
            if (BlockUpdates()) return;
            base.UpdateAfterSimulation();
            UpdateAfterSim();
        }

        /// <summary>Virtual hook called every tick in UpdateAfterSimulation.</summary>
        protected virtual void UpdateAfterSim() { }

        /// <summary>Delegates to the virtual <see cref="Unload"/> then calls the base.</summary>
        protected override void UnloadData()
        {
            Unload();
            base.UnloadData();
        }

        /// <summary>
        /// Override in subclasses to clean up resources.  The base implementation
        /// closes the log file and resets setup flags so that a subsequent world load
        /// on the same instance (SE reuses session component instances) runs all setup
        /// phases again and creates a fresh log.
        /// Guards against running on the wrong network side.
        /// </summary>
        protected virtual void Unload()
        {
            if (BlockUpdates()) return;
            // Use the Close(caller, message) overload so the final line is written synchronously
            // before the TextWriter is disposed.  WriteGeneral routes through InvokeOnGameThread
            // and would be silently dropped after Close() nulls the writer.
            _generalLog?.Close($"{CompName}: Unload", "Retired.");
            _generalLog = null;
            // Reset so SuperEarlySetup / EarlySetup / LateSetup all re-run on the next LoadData.
            _superEarlySetupComplete = false;
            _earlySetupComplete = false;
            _lateSetupComplete = false;
        }

        /// <summary>
        ///  Gets called 60 times a second before all other update methods, regardless of frame rate, game pause or MyUpdateOrder.
        /// </summary>
        public override void HandleInput()
        {
            base.HandleInput();
        }

        /// <summary>
        ///  Executed every tick, 60 times a second, during physics simulation and only if game is not paused.
        ///  NOTE: In this example this won't actually be called because of the lack of MyUpdateOrder.Simulation argument in MySessionComponentDescriptor
        /// </summary>
        public override void Simulate()
        {
            base.Simulate();
        }

        /// <summary>
        ///  Gets called 60 times a second after all other update methods, regardless of framerate, game pause or MyUpdateOrder.
        ///  NOTE: This is the only place where the camera matrix (MyAPIGateway.Session.Camera.WorldMatrix) is accurate, everywhere else it's 1 frame behind.
        /// </summary>
        public override void Draw()
        {
            base.Draw();
        }

        /// <summary>
        ///  Executed when game is paused
        /// </summary>
        public override void UpdatingStopped()
        {
            base.UpdatingStopped();
        }

        /// <summary>
        /// Writes an exception-level log line via the session log.
        /// The <c>?.</c> null-conditional guard makes this safe to call even if the log
        /// has been closed during teardown.
        /// </summary>
        public void WriteException(string caller, string message)
        {
            _generalLog?.WriteException($"{CompName}: {caller}", message);
        }

        /// <summary>
        /// Writes a general log line via the session log.
        /// The <c>?.</c> null-conditional guard makes this safe to call even after
        /// <see cref="Log.Close"/> has been called.
        /// </summary>
        public void WriteGeneral(string caller = "", string message = "")
        {
            _generalLog?.WriteGeneral($"{CompName}: {caller}", message);
        }
    }
}
