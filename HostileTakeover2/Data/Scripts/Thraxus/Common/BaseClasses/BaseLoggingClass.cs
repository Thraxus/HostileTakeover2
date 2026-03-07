using System;
using HostileTakeover2.Thraxus.Common.Interfaces;

namespace HostileTakeover2.Thraxus.Common.BaseClasses
{
    /// <summary>
    /// Abstract base class for every loggable object in the mod.
    ///
    /// Provides a common event chain (<see cref="OnWriteToLog"/>) that propagates log
    /// messages up the object graph to the session-level <see cref="Log"/>.  Consumers
    /// subscribe to <c>OnWriteToLog</c> and forward it to their own parent; ultimately
    /// the chain terminates at <c>BaseSessionComp.WriteGeneral</c> which writes to the
    /// mod log file.
    ///
    /// Also exposes <see cref="OnClose"/> and <see cref="OnReset"/> events so that
    /// object-pool managers and other observers can react to lifecycle transitions
    /// without needing direct references.
    ///
    /// The <c>IsClosed</c> flag is used throughout the codebase as a guard to prevent
    /// processing after an object has been returned to its pool or destroyed.
    /// </summary>
    public abstract class BaseLoggingClass : ICommon
    {
        /// <summary>
        /// Event fired whenever a log line is produced by this object or one of its
        /// children.  Subscribers (typically the parent object or the Mediator) forward
        /// the message further up the chain.
        /// Parameters: (caller, message).
        /// </summary>
        public event Action<string, string> OnWriteToLog;

        /// <summary>
        /// Fired when this object transitions to the closed state via <see cref="Close"/>.
        /// Pool managers subscribe to return the object to the pool.
        /// </summary>
        public event Action<IClose> OnClose;

        /// <summary>
        /// Fired when this object is being reset (returned to a pool or reinitialised).
        /// Subscribers use this to clean up their own references to this object.
        /// </summary>
        public event Action<IResetWithAction> OnReset;

        /// <summary>
        /// Optional prefix appended to every log line emitted by this instance.
        /// Set via <see cref="SetLogPrefix"/> to scope log output to a specific entity
        /// (e.g. the entity ID of a grid).
        /// </summary>
        private string _logPrefix;

        /// <summary>
        /// Sets the log prefix string.  All subsequent <see cref="WriteGeneral"/> calls
        /// from this object will include the prefix in the caller field.
        /// </summary>
        protected void SetLogPrefix(string prefix)
        {
            _logPrefix = "[" + prefix + "] ";
        }

        /// <summary>
        /// True after <see cref="Close"/> has been called or the object has been
        /// returned to a pool.  Used as a guard throughout the codebase to skip
        /// processing on objects that are no longer active.
        /// </summary>
        public bool IsClosed { get; protected set; }

        /// <summary>
        /// Marks the object as closed and fires <see cref="OnClose"/> so that any
        /// registered pool managers or observers can react.  Idempotent: subsequent
        /// calls after the first are no-ops.
        /// </summary>
        public virtual void Close()
        {
            if (IsClosed) return;
            IsClosed = true;
            OnClose?.Invoke(this);
        }

        /// <summary>Virtual tick hook; not used by most subclasses.</summary>
        public virtual void Update(ulong tick) { }

        /// <summary>
        /// Emits a log line by raising <see cref="OnWriteToLog"/>.  The
        /// <c>_logPrefix</c> is prepended to <paramref name="caller"/> so log output
        /// is scoped to the specific instance.  If no subscriber is attached the event
        /// is safely no-op'd.
        /// </summary>
        public virtual void WriteGeneral(string caller, string message)
        {
            OnWriteToLog?.Invoke($"{_logPrefix}{caller}", message);
        }

        /// <summary>
        /// Category-tagged log emission.  Prepends <c>[type]</c> to the caller field
        /// so log lines are filterable by subsystem.  Delegates to the virtual
        /// <see cref="WriteGeneral(string,string)"/> so the correct override (e.g. the
        /// BaseSessionComp terminal sink) is invoked throughout the class hierarchy.
        /// </summary>
        protected void WriteGeneral(LogCategory type, string caller, string message)
        {
            WriteGeneral($"[{type}] {caller}", message);
        }

        /// <summary>
        /// Fires <see cref="OnReset"/> to notify observers that this object is being
        /// reset.  Subclasses override this to clear their own state before calling
        /// <c>base.Reset()</c>.
        /// </summary>
        public virtual void Reset()
        {
            OnReset?.Invoke(this);
        }
    }
}
