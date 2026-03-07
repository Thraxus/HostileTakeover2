using System;
using System.Collections.Generic;

namespace HostileTakeover2.Thraxus.Common.Generics
{
    /// <summary>
    /// Tick-based deferred-action scheduler.  Callers add an <see cref="Action"/> with a
    /// delay (in ticks); the queue executes all due actions once per tick when
    /// <see cref="Execute"/> is called from the session component's UpdateBeforeSim.
    ///
    /// Internally uses a <see cref="SortedList{TKey,TValue}"/> keyed by target tick so
    /// that <see cref="Execute"/> can stop iterating as soon as it reaches a bucket whose
    /// key is in the future — O(log n) insertion, O(1) peek at the next-due bucket.
    /// </summary>
    public class ActionQueue
    {
        // Keyed by the target tick at which actions become due.
        // SortedList keeps keys in ascending order so Execute() can stop early.
        private readonly SortedList<int, List<Action>> _scheduledActions = new SortedList<int, List<Action>>();
        // Monotonically increasing counter advanced by Execute() on each call.
        private int _currentTick = 0;
        // Diagnostic: counts all actions invoked (deferred + immediate) in the current 120-tick window.
        private int _windowActionCount = 0;
        // Invoked every 120 ticks with (context, message) matching the WriteGeneral signature.
        public Action<string, string> OnReport;

        /// <summary>
        /// Schedule an action to run after <paramref name="delay"/> calls to Execute().
        /// A delay of 0 (or negative) runs the action immediately (synchronously) rather
        /// than inserting it into the schedule — this avoids the overhead of a
        /// dictionary round-trip for fire-and-forget calls.
        /// </summary>
        public void Add(int delay, Action action)
        {
            if (delay <= 0)
            {
                // Immediate invocation: bypass the scheduler entirely and call inline.
                try { action?.Invoke(); }
                catch { /* prevent a bad action from crashing the game */ }
                _windowActionCount++;
                return;
            }

            // Calculate the absolute tick at which this action becomes due.
            int targetTick = _currentTick + delay;
            List<Action> bucket;
            if (!_scheduledActions.TryGetValue(targetTick, out bucket))
            {
                bucket = new List<Action>();
                _scheduledActions[targetTick] = bucket;
            }
            bucket.Add(action);
        }

        /// <summary>
        /// Advances the internal tick counter by one and invokes all actions in every
        /// bucket whose target tick is now &lt;= the current tick.  Processed buckets are
        /// removed from the sorted list immediately.
        ///
        /// <paramref name="iterationMax"/> is a safety cap on the total number of actions
        /// executed per call; if hit, remaining actions are left in the queue and will be
        /// processed on subsequent ticks.  This prevents a single overloaded tick from
        /// stalling the simulation.
        /// </summary>
        public void Execute(int iterationMax = 500)
        {
            _currentTick++;
            if (_currentTick % 120 == 0)
            {
                OnReport?.Invoke(nameof(ActionQueue), $"Actions executed in last 120 ticks: {_windowActionCount}");
                _windowActionCount = 0;
            }
            int processed = 0;

            // The SortedList is ordered ascending by target tick; the first entry is always
            // the soonest-due bucket.  Stop as soon as the next bucket is in the future.
            while (_scheduledActions.Count > 0 && _scheduledActions.Keys[0] <= _currentTick)
            {
                List<Action> actions = _scheduledActions.Values[0];
                // Remove the bucket before invoking so re-entrant Add calls don't corrupt iteration.
                _scheduledActions.RemoveAt(0);

                for (int i = 0; i < actions.Count; i++)
                {
                    try { actions[i]?.Invoke(); }
                    catch { /* prevent a bad action from crashing the game */ }
                    _windowActionCount++;
                    // Enforce the per-call action cap.  When hit, reschedule any remaining
                    // actions in this bucket for the very next tick so they are not lost.
                    if (++processed >= iterationMax)
                    {
                        if (i + 1 < actions.Count)
                        {
                            int nextTick = _currentTick + 1;
                            List<Action> overflow;
                            if (!_scheduledActions.TryGetValue(nextTick, out overflow))
                            {
                                overflow = new List<Action>();
                                _scheduledActions[nextTick] = overflow;
                            }
                            for (int j = i + 1; j < actions.Count; j++)
                                overflow.Add(actions[j]);
                        }
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Clears all pending actions and resets the tick counter to zero.
        /// Intended for session teardown.
        /// </summary>
        public void Reset()
        {
            _scheduledActions.Clear();
            _currentTick = 0;
            _windowActionCount = 0;
            OnReport = null;
        }
    }
}
