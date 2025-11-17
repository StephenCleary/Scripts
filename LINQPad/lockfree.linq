<Query Kind="Program">
  <Namespace>System.Collections.Immutable</Namespace>
</Query>

#nullable enable

void Main()
{
}



public sealed record class ImmutableDeque<T>
{
	public static ImmutableDeque<T> Empty { get; } = new(Node0.Instance, ImmutableList<T>.Empty, Node0.Instance);

	public bool IsEmpty => _left.IsEmpty && _mid.IsEmpty && _right.IsEmpty;

	public ImmutableDeque<T> EnqueueFront(T value)
	{
		var newLeft = _left.TryEnqueueFront(value);
		if (newLeft != null)
			return new(newLeft, _mid, _right);
		
		return new (new Node1(value), _mid.InsertRange(0, _left), _right);
	}

	public ImmutableDeque<T> EnqueueBack(T value)
	{
		var newRight = _right.TryEnqueueBack(value);
		if (newRight != null)
			return new(_left, _mid, newRight);
			
		return new (_left, _mid.InsertRange(_mid.Count, _right), new Node1(value));
	}

	public ImmutableDeque<T> DequeueFront(out T value)
	{
		{
			var newLeft = _left.TryDequeueFront(out value);
			if (newLeft != null)
				return new(newLeft, _mid, _right);
		}

		{
			var newLeft = TrySpliceFromMid(out var newMid, out value);
			if (newLeft != null)
				return new(newLeft, newMid, _right);
		}

		{
			var newRight = _right.TryDequeueFront(out value);
			if (newRight != null)
				return new(_left, _mid, newRight);
		}
		
		throw new InvalidOperationException("Deque is empty");

		INode? TrySpliceFromMid(out ImmutableList<T> newMid, out T value)
		{
			Span<T> temp = new T[4];
			int count = 0;
			foreach (var item in _mid)
			{
				temp[count] = item;
				++count;
				if (count == 4)
					break;
			}

			if (count == 0)
			{
				newMid = default!;
				value = default!;
				return null;
			}

			value = temp[0];
			newMid = count == 4 ? _mid.RemoveRange(0, 4) : ImmutableList<T>.Empty;
			return count switch
			{
				1 => Node0.Instance,
				2 => new Node1(temp[1]),
				3 => new Node2(temp[1], temp[2]),
				4 => new Node3(temp[1], temp[2], temp[3]),
			};
		}
	}

	public ImmutableDeque<T> DequeueBack(out T value)
	{
		{
			var newRight = _right.TryDequeueBack(out value);
			if (newRight != null)
				return new(_left, _mid, newRight);
		}

		{
			var newRight = TrySpliceFromMid(out var newMid, out value);
			if (newRight != null)
				return new(_left, newMid, newRight);
		}

		{
			var newLeft = _left.TryDequeueBack(out value);
			if (newLeft != null)
				return new(newLeft, _mid, _right);
		}

		throw new InvalidOperationException("Deque is empty");

		INode? TrySpliceFromMid(out ImmutableList<T> newMid, out T value)
		{
			var count = Math.Min(_mid.Count, 4);
			var splice = count == _mid.Count ? _mid : _mid.GetRange(_mid.Count - 4, 4);
			
			Span<T> temp = splice.ToArray();

			if (count == 0)
			{
				newMid = default!;
				value = default!;
				return null;
			}

			value = temp[count - 1];
			newMid = count == 4 ? _mid.RemoveRange(_mid.Count - 4, 4) : ImmutableList<T>.Empty;
			return count switch
			{
				1 => Node0.Instance,
				2 => new Node1(temp[0]),
				3 => new Node2(temp[0], temp[1]),
				4 => new Node3(temp[0], temp[1], temp[2]),
			};
		}
	}

	public ImmutableDeque<T>? RemoveOne(Func<T, bool> predicate, out T value)
	{
		{
			var newLeft = _left.RemoveOne(predicate, out value);
			if (newLeft != null)
				return new(newLeft, _mid, _right);
		}
		{
			var newRight = _right.RemoveOne(predicate, out value);
			if (newRight != null)
				return new(_left, _mid, newRight);
		}
		{
			var index = _mid.FindIndex(x => predicate(x));
			if (index != -1)
			{
				value = _mid[index];
				return new(_left, _mid.RemoveAt(index), _right);
			}
		}
		
		value = default!;
		return null;
	}

	private ImmutableDeque(INode left, ImmutableList<T> mid, INode right)
	{
		_left = left;
		_mid = mid;
		_right = right;
	}
	
	private readonly INode _left;
	private readonly ImmutableList<T> _mid;
	private readonly INode _right;
	
	private interface INode : IEnumerable<T>
	{
		bool IsEmpty { get; }
		INode? TryEnqueueFront(T value);
		INode? TryEnqueueBack(T value);
		INode? TryDequeueFront(out T value);
		INode? TryDequeueBack(out T value);
		INode? RemoveOne(Func<T, bool> predicate, out T value);
	}
	
	private sealed record class Node0() : INode
	{
		public static Node0 Instance { get; } = new();

		public bool IsEmpty => true;
		public INode? TryEnqueueFront(T value) => new Node1(value);
		public INode? TryEnqueueBack(T value) => new Node1(value);
		public INode? TryDequeueFront(out T value)
		{
			value = default!;
			return null;
		}
		public INode? TryDequeueBack(out T value)
		{
			value = default!;
			return null;
		}
		public INode? RemoveOne(Func<T, bool> predicate, out T value)
		{
			value = default!;
			return null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield break;
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
	private sealed record class Node1(T Value1) : INode
	{
		public bool IsEmpty => false;
		public INode? TryEnqueueFront(T value) => new Node2(value, Value1);
		public INode? TryEnqueueBack(T value) => new Node2(Value1, value);
		public INode? TryDequeueFront(out T value)
		{
			value = Value1;
			return Node0.Instance;
		}
		public INode? TryDequeueBack(out T value)
		{
			value = Value1;
			return Node0.Instance;
		}
		public INode? RemoveOne(Func<T, bool> predicate, out T value)
		{
			var test = predicate(Value1);
			value = test ? Value1 : default!;
			return test ? Node0.Instance : null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield return Value1;
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
	private sealed record class Node2(T Value1, T Value2) : INode
	{
		public bool IsEmpty => false;
		public INode? TryEnqueueFront(T value) => new Node3(value, Value1, Value2);
		public INode? TryEnqueueBack(T value) => new Node3(Value1, Value2, value);
		public INode? TryDequeueFront(out T value)
		{
			value = Value1;
			return new Node1(Value2);
		}
		public INode? TryDequeueBack(out T value)
		{
			value = Value2;
			return new Node1(Value1);
		}
		public INode? RemoveOne(Func<T, bool> predicate, out T value)
		{
			{
				var test = predicate(Value1);
				if (test)
				{
					value = Value1;
					return new Node1(Value2);
				}
			}
			{
				var test = predicate(Value2);
				if (test)
				{
					value = Value2;
					return new Node1(Value1);
				}
			}
			value = default!;
			return null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield return Value1;
			yield return Value2;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
	private sealed record class Node3(T Value1, T Value2, T Value3) : INode
	{
		public bool IsEmpty => false;
		public INode? TryEnqueueFront(T value) => new Node4(value, Value1, Value2, Value3);
		public INode? TryEnqueueBack(T value) => new Node4(Value1, Value2, Value3, value);
		public INode? TryDequeueFront(out T value)
		{
			value = Value1;
			return new Node2(Value2, Value3);
		}
		public INode? TryDequeueBack(out T value)
		{
			value = Value3;
			return new Node2(Value1, Value2);
		}
		public INode? RemoveOne(Func<T, bool> predicate, out T value)
		{
			{
				var test = predicate(Value1);
				if (test)
				{
					value = Value1;
					return new Node2(Value2, Value3);
				}
			}
			{
				var test = predicate(Value2);
				if (test)
				{
					value = Value2;
					return new Node2(Value1, Value3);
				}
			}
			{
				var test = predicate(Value3);
				if (test)
				{
					value = Value3;
					return new Node2(Value1, Value2);
				}
			}
			value = default!;
			return null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield return Value1;
			yield return Value2;
			yield return Value3;
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
	private sealed record class Node4(T Value1, T Value2, T Value3, T Value4) : INode
	{
		public bool IsEmpty => false;
		public INode? TryEnqueueFront(T value) => null;
		public INode? TryEnqueueBack(T value) => null;
		public INode? TryDequeueFront(out T value)
		{
			value = Value1;
			return new Node3(Value2, Value3, Value4);
		}
		public INode? TryDequeueBack(out T value)
		{
			value = Value4;
			return new Node3(Value1, Value2, Value3);
		}
		public INode? RemoveOne(Func<T, bool> predicate, out T value)
		{
			{
				var test = predicate(Value1);
				if (test)
				{
					value = Value1;
					return new Node3(Value2, Value3, Value4);
				}
			}
			{
				var test = predicate(Value2);
				if (test)
				{
					value = Value2;
					return new Node3(Value1, Value3, Value4);
				}
			}
			{
				var test = predicate(Value3);
				if (test)
				{
					value = Value3;
					return new Node3(Value1, Value2, Value4);
				}
			}
			{
				var test = predicate(Value4);
				if (test)
				{
					value = Value4;
					return new Node3(Value1, Value2, Value3);
				}
			}
			value = default!;
			return null;
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield return Value1;
			yield return Value2;
			yield return Value3;
			yield return Value4;
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}

/// <summary>
/// Interlocked helper methods.
/// </summary>
public static class InterlockedState
{
	/// <summary>
	/// Executes a state transition from one state to another.
	/// </summary>
	/// <typeparam name="T">The type of the state; this is generally an immutable type. Strongly consider using a record class.</typeparam>
	/// <param name="state">The location of the state.</param>
	/// <param name="transformation">The transformation to apply to the state. This may be invoked any number of times and should be a pure function.</param>
	/// <returns>The old state and the new state.</returns>
	public static (T OldState, T NewState) Transform<T>(ref T state, Func<T, T> transformation)
		where T : class?
	{
		_ = transformation ?? throw new ArgumentNullException(nameof(transformation));

		while (true)
		{
			var oldState = Interlocked.CompareExchange(ref state, default!, default!);
			var newState = transformation(oldState);
			if (ReferenceEquals(Interlocked.CompareExchange(ref state, newState, oldState), oldState))
				return (oldState, newState);
		}
	}

	/// <summary>
	/// Executes a state transition from one state to another.
	/// </summary>
	/// <param name="state">The location of the state.</param>
	/// <param name="transformation">The transformation to apply to the state. This may be invoked any number of times and should be a pure function.</param>
	/// <returns>The old state and the new state.</returns>
	public static (int OldState, int NewState) Transform(ref int state, Func<int, int> transformation)
	{
		_ = transformation ?? throw new ArgumentNullException(nameof(transformation));

		while (true)
		{
			var oldState = Interlocked.CompareExchange(ref state, default!, default!);
			var newState = transformation(oldState);
			if (Interlocked.CompareExchange(ref state, newState, oldState) == oldState)
				return (oldState, newState);
		}
	}

	/// <inheritdoc cref="Transform(ref int, Func{int, int})"/>
	public static (uint OldState, uint NewState) Transform(ref uint state, Func<uint, uint> transformation)
	{
		_ = transformation ?? throw new ArgumentNullException(nameof(transformation));

		while (true)
		{
			var oldState = Interlocked.CompareExchange(ref state, default!, default!);
			var newState = transformation(oldState);
			if (Interlocked.CompareExchange(ref state, newState, oldState) == oldState)
				return (oldState, newState);
		}
	}

	/// <inheritdoc cref="Transform(ref int, Func{int, int})"/>
	public static (long OldState, long NewState) Transform(ref long state, Func<long, long> transformation)
	{
		_ = transformation ?? throw new ArgumentNullException(nameof(transformation));

		while (true)
		{
			var oldState = Interlocked.CompareExchange(ref state, default!, default!);
			var newState = transformation(oldState);
			if (Interlocked.CompareExchange(ref state, newState, oldState) == oldState)
				return (oldState, newState);
		}
	}

	/// <inheritdoc cref="Transform(ref int, Func{int, int})"/>
	public static (ulong OldState, ulong NewState) Transform(ref ulong state, Func<ulong, ulong> transformation)
	{
		_ = transformation ?? throw new ArgumentNullException(nameof(transformation));

		while (true)
		{
			var oldState = Interlocked.CompareExchange(ref state, default!, default!);
			var newState = transformation(oldState);
			if (Interlocked.CompareExchange(ref state, newState, oldState) == oldState)
				return (oldState, newState);
		}
	}

	/// <summary>
	/// Reads the current state. Note that the state may have changed by the time this method returns.
	/// </summary>
	/// <typeparam name="T">The type of the state; this is generally an immutable type. Strongly consider using a record class.</typeparam>
	/// <param name="state">The location of the state.</param>
	/// <returns>The current state.</returns>
	public static T Read<T>(ref T state)
		where T : class? =>
		Interlocked.CompareExchange(ref state, default!, default!);

	/// <summary>
	/// Reads the current state. Note that the state may have changed by the time this method returns.
	/// </summary>
	/// <param name="state">The location of the state.</param>
	/// <returns>The current state.</returns>
	public static int Read(ref int state) => Interlocked.CompareExchange(ref state, default!, default!);

	/// <inheritdoc cref="Read(ref int)"/>
	public static uint Read(ref uint state) => Interlocked.CompareExchange(ref state, default!, default!);

	/// <inheritdoc cref="Read(ref int)"/>
	public static long Read(ref long state) => Interlocked.CompareExchange(ref state, default!, default!);

	/// <inheritdoc cref="Read(ref int)"/>
	public static ulong Read(ref ulong state) => Interlocked.CompareExchange(ref state, default!, default!);
}