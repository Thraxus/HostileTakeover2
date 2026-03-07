using System;
using System.Collections.Concurrent;
using HostileTakeover2.Thraxus.Common.Interfaces;

namespace HostileTakeover2.Thraxus.Common.Generics
{
    /// <summary>
    /// Generic thread-safe object pool for types that implement <see cref="IReset"/>.
    ///
    /// Backed by a <see cref="ConcurrentBag{T}"/> which provides lock-free
    /// TryTake / Add operations suitable for multi-threaded access without requiring
    /// external synchronisation.
    ///
    /// Lifecycle contract:
    /// <list type="number">
    ///   <item>Call <see cref="Get"/> to borrow an instance (creates one via the generator
    ///     if the bag is empty).</item>
    ///   <item>Use the instance.</item>
    ///   <item>Call <see cref="Return"/> when done — <see cref="IReset.Reset"/> is invoked
    ///     automatically before the object is placed back so stale state is never carried
    ///     over to the next borrower.</item>
    /// </list>
    /// </summary>
    internal class ObjectPool<T> where T : IReset
    {
        // ConcurrentBag is used for its thread-safe, allocation-light TryTake/Add pattern.
        private readonly ConcurrentBag<T> _objects;
        // Factory delegate supplied at construction time; called only when the bag is empty.
        private readonly Func<T> _objectGenerator;

        /// <summary>
        /// Creates a new pool with the provided <paramref name="objectGenerator"/> factory.
        /// </summary>
        public ObjectPool(Func<T> objectGenerator)
        {
            _objectGenerator = objectGenerator;
            _objects = new ConcurrentBag<T>();
        }

        /// <summary>Returns the number of objects currently sitting idle in the pool.</summary>
        public int Count() => _objects.Count;

        /// <summary>Running total of all successful <see cref="Get"/> calls (pool hits + misses).</summary>
        public long TotalObjectsServed;

        /// <summary>
        /// Number of times <see cref="Get"/> had to call the generator because the bag was
        /// empty.  High values indicate the pool size should be pre-warmed.
        /// </summary>
        public long TotalAllocations;

        /// <summary>
        /// Borrows an object from the pool.  If the pool is empty a new instance is
        /// created via the generator delegate.  The returned object has already been
        /// <c>Reset()</c>-ed (from its previous <see cref="Return"/> call).
        /// </summary>
        public T Get()
        {
            T item;
            TotalObjectsServed++;
            // TryTake is a lock-free attempt to pop from the bag.
            if (_objects.TryTake(out item)) return item;
            // Pool miss: allocate a fresh instance and count it for diagnostics.
            TotalAllocations++;
            return _objectGenerator();
        }

        /// <summary>
        /// Returns <paramref name="item"/> to the pool.  <see cref="IReset.Reset"/> is
        /// called on the item before it is added so no caller-state leaks to the next
        /// borrower.
        /// </summary>
        public void Return(T item)
        {
            item.Reset();
            _objects.Add(item);
        }

        /// <summary>
        /// Diagnostic string showing pool type, total served, max ever created, and
        /// current idle count.
        /// </summary>
        public override string ToString()
        {
            return $"PoolType: [{typeof(T).Name}] Total Served: [{TotalObjectsServed:D4}] Max Created: [{TotalAllocations:D4}] Current Pooled: [{Count():D4}]";
        }
    }
}

/*
 * Example Usage:
 * private GenericObjectPool<GrindOperation> _grindOperations = new GenericObjectPool<GrindOperation>(() => new GrindOperation(_userSettings));
 *
 * GrindOperation op =  _grindOperations.Get();
 *
		private void ClearGrindOperationsPool()
		{
			for (int i = _pooledGrindOperations.Count - 1; i >= 0; i--)
			{
				if (_pooledGrindOperations[i].Tick == TickCounter) continue;
				GrindOperation op = _pooledGrindOperations[i];
				_pooledGrindOperations.RemoveAtImmediately(i);
				op.OnWriteToLog -= WriteToLog;
				op.Reset();
				_grindOperations.Return(op);
			}
			_pooledGrindOperations.ApplyRemovals();
		}
 *
 * Make sure to clean up the object before returning it to the pool, else you may carry over obsolete / incorrect info
 *
 */
