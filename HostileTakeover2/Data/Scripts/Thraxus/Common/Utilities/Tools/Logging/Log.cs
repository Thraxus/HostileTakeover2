using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Logging
{
    /// <summary>
    /// Lightweight mod-local log file writer.  Each instance owns a single <c>.log</c>
    /// file in the mod's local storage directory (separate from the world save directory).
    ///
    /// Thread safety: <see cref="BuildLogLine"/> acquires <c>_lockObject</c> before
    /// dispatching to the game thread via <c>InvokeOnGameThread</c>.  This ensures that
    /// callers from background threads do not race on the <see cref="TextWriter"/> and
    /// that the actual write always occurs on the game's main thread (required by the
    /// Space Engineers API).
    ///
    /// Dual output: every line is written both to the mod-local file and to
    /// <c>MyLog.Default</c> (the game's shared log) so issues are visible in both the
    /// mod-specific file and the global game log.
    /// </summary>
    public class Log
    {
        private string LogName { get; set; }

        /// <summary>
        /// Underlying file writer.  Null after <see cref="Close"/> is called.
        /// All writes are guarded by a null-conditional (<c>?.</c>) so no explicit
        /// null checks are required at each call site.
        /// </summary>
        private TextWriter TextWriter { get; set; }

        /// <summary>High-resolution timestamp prepended to every log line.</summary>
        private static string TimeStamp => DateTime.Now.ToString("ddMMMyy_HH:mm:ss:ffff");

        private const int DefaultIndent = 4;

        /// <summary>Spacing string inserted between timestamp, caller, and message fields.</summary>
        private static string Indent { get; } = new string(' ', DefaultIndent);

        /// <summary>
        /// Creates the log and immediately opens the underlying file via
        /// <see cref="MyAPIGateway.Utilities.WriteFileInLocalStorage"/>.
        /// Local storage is mod-specific and is not shared with other mods.
        /// </summary>
        public Log(string logName)
        {
            LogName = logName + ".log";
            Init();
        }

        /// <summary>
        /// Opens (or re-opens) the log file.  The null guard prevents opening the file
        /// twice if Init is called more than once on the same instance.
        /// </summary>
        private void Init()
        {
            if (TextWriter != null) return;
            // WriteFileInLocalStorage creates or truncates the file in the mod's private storage area.
            TextWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage(LogName, typeof(Log));
        }

        /// <summary>
        /// Writes a final log line synchronously (bypassing <c>InvokeOnGameThread</c>) then
        /// flushes and closes the underlying writer.  Use this overload in teardown paths
        /// where the closing message must reach the file before the writer is disposed.
        /// </summary>
        public void Close(string caller, string message)
        {
            var newMessage = $"{TimeStamp}{Indent}{caller}{Indent}{message}";
            TextWriter?.WriteLine(newMessage);
            MyLog.Default.WriteLineAndConsole(newMessage);
            Close();
        }

        /// <summary>
        /// Flushes and closes the underlying writer then nulls the reference so that
        /// subsequent <see cref="WriteLine"/> calls (guarded by <c>?.</c>) become no-ops.
        /// </summary>
        public void Close()
        {
            TextWriter?.Flush();
            TextWriter?.Dispose();  
            TextWriter?.Close(); // Dispose() calls Close() internally; but is not reliable, so ensure this is closed.
            TextWriter = null;
        }

        /// <summary>Writes a general informational log line.</summary>
        public void WriteGeneral(string caller = "", string message = "")
        {
            BuildLogLine(caller, message);
        }

        /// <summary>Writes an exception log line with a prominent "Exception!" prefix.</summary>
        public void WriteException(string caller = "", string message = "")
        {
            BuildLogLine(caller, "Exception!\n\n" + message);
        }

        // Lock object used to serialise access from multiple threads before the
        // InvokeOnGameThread dispatch.
        private readonly object _lockObject = new object();

        /// <summary>
        /// Formats a log line and dispatches it to the game thread for writing.
        /// The lock ensures that concurrent callers from different threads do not
        /// interleave their InvokeOnGameThread queuing.  The actual file write and
        /// <c>MyLog.Default</c> write occur on the game thread as required by the API.
        /// </summary>
        private void BuildLogLine(string caller, string message)
        {
            lock (_lockObject)
            {
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    var newMessage = $"{TimeStamp}{Indent}{caller}{Indent}{message}";
                    // Write to the mod-local log file.
                    WriteLine(newMessage);
                    // Also write to the game's shared log so the line appears in SpaceEngineers.log.
                    MyLog.Default.WriteLineAndConsole(newMessage);
                });
            }
        }

        /// <summary>
        /// Writes a single line to the underlying <see cref="TextWriter"/> and flushes
        /// immediately so the line is visible on disk even if the game crashes before a
        /// normal shutdown.  The null-conditional guard makes this safe to call after
        /// <see cref="Close"/> has been called.
        /// </summary>
        private void WriteLine(string line)
        {
            TextWriter?.WriteLine(line);
            TextWriter?.Flush();
        }
    }
}
