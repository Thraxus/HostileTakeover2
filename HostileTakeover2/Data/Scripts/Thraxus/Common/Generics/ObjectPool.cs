using System;
using System.Collections.Concurrent;
using HostileTakeover2.Thraxus.Common.Interfaces;

namespace HostileTakeover2.Thraxus.Common.Generics
{
    internal class ObjectPool<T> where T : IReset
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;
        
        public ObjectPool(Func<T> objectGenerator)
        {
            _objectGenerator = objectGenerator;
            _objects = new ConcurrentBag<T>();
        }

        public int Count() => _objects.Count;
        public long TotalObjectsServed;
        public long MaxNewObjects;

        public T Get()
        {
            T item;
            TotalObjectsServed++;
            if (_objects.TryTake(out item)) return item;
            MaxNewObjects++;
            return _objectGenerator();
        }

        public void Return(T item)
        {
            item.Reset();
            _objects.Add(item);
        }

        public override string ToString()
        {
            return $"PoolType: [{typeof(T).Name}] Total Served: [{TotalObjectsServed:D4}] Max Created: [{MaxNewObjects:D4}] Current Pooled: [{Count():D4}]";
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