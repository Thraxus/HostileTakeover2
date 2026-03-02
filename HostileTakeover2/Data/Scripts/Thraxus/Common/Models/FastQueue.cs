
namespace HostileTakeover2.Thraxus.Common.Models
{
	/// <summary>
	/// Fixed-capacity circular (ring) buffer queue with O(1) enqueue and dequeue.
	/// The backing array is allocated once at construction; no further heap allocation
	/// occurs during normal use.
	///
	/// <c>_current</c> is the read head (next item to dequeue).
	/// <c>_emptySpot</c> is the write head (next slot to fill on enqueue).
	/// Both heads wrap around modulo the array length to form the ring.
	///
	/// <c>Count</c> reflects the number of items actually in the queue (not the
	/// backing-array length), and <see cref="Enqueue"/> throws if the queue is full
	/// rather than silently overwriting unread data.
	/// </summary>
	public class FastQueue<T>
	{
		/// <summary>The fixed-size backing array that forms the ring buffer.</summary>
		private readonly T[] _nodes;
		/// <summary>Read head: index of the next item to be returned by <see cref="Dequeue"/>.</summary>
		private int _current;
		/// <summary>Write head: index of the next slot to be written by <see cref="Enqueue"/>.</summary>
		private int _emptySpot;
		/// <summary>Actual number of items currently in the queue.</summary>
		private int _count;

		/// <summary>
		/// Allocates the backing array and initialises both heads to position 0.
		/// </summary>
		public FastQueue(int size)
		{
			_nodes = new T[size];
			_current = 0;
			_emptySpot = 0;
			_count = 0;
		}

		/// <summary>
		/// Adds <paramref name="value"/> to the back of the queue.
		/// Throws <see cref="System.InvalidOperationException"/> if the queue is at
		/// capacity to prevent silent data loss from buffer wrap-around.
		/// </summary>
		public void Enqueue(T value)
		{
			if (_count >= _nodes.Length)
				throw new System.InvalidOperationException($"FastQueue is full (capacity {_nodes.Length}). Cannot enqueue further items.");
			_nodes[_emptySpot] = value;
			_emptySpot++;
			// Wrap the write head around to the start of the array.
			if (_emptySpot >= _nodes.Length)
				_emptySpot = 0;
			_count++;
		}

		/// <summary>
		/// Removes and returns the item at the front of the queue.
		/// Does not bounds-check for an empty queue; callers should verify
		/// <see cref="Count"/> &gt; 0 before calling.
		/// </summary>
		public T Dequeue()
		{
			int ret = _current;
			_current++;
			// Wrap the read head around to the start of the array.
			if (_current >= _nodes.Length)
				_current = 0;
			_count--;
			return _nodes[ret];
		}

		/// <summary>
		/// Returns the raw backing array.  Items are not necessarily contiguous from
		/// index 0; use <see cref="Dequeue"/> for ordered access.
		/// </summary>
		public T[] GetQueue()
		{
			return _nodes;
		}

		/// <summary>Number of items currently held in the queue.</summary>
		public int Count => _count;

		/// <summary>
		/// Zeros all slots in the backing array and resets both heads and the item
		/// count to 0, leaving the queue empty but with its original capacity.
		/// </summary>
		public void Clear()
		{
			for (int index = _nodes.Length - 1; index >= 0; index--)
				_nodes[index] = default(T);
			_current = 0;
			_emptySpot = 0;
			_count = 0;
		}
	}
}
