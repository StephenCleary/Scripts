<Query Kind="Program">
  <NuGetReference>Nito.Disposables</NuGetReference>
  <Namespace>System.ComponentModel</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>Nito.Disposables</Namespace>
  <Namespace>System.Runtime.ExceptionServices</Namespace>
</Query>

#nullable enable

void Main()
{
	var m = new Mmmm();
	_ = m.Subscribe(x => x.Next!.Next!.Test, (ex, x) => (ex, x).Dump());
	m.Next = new();
	m.Next.Next = new();
	m.Next.Next.Test = 31;
	m.Next.Next = null;
	m.Next.Next = new() { Test = 13 };
}

public sealed class Mmmm : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	public Mmmm? Next
	{
		get => _next; set
		{
			_next = value;
			PropertyChanged?.Invoke(this, new(nameof(Next)));
		}
	}

	private Mmmm? _next;

	public int Test
	{
		get => _test;
		set
		{
			_test = value;
			PropertyChanged?.Invoke(this, new(nameof(Test)));
		}
	}
	
	private int _test;
}

public static class Bob
{
	public static IDisposable Subscribe<TSource, TResult>(this TSource source, Expression<Func<TSource, TResult>> expression,
		Action<Exception?, TResult?> onChanged, IEqualityComparer<TResult>? equalityComparer = null, TResult? valueIfNull = default)
		where TSource : INotifyPropertyChanged
	{
		Expression current = expression.Body;
		List<PropertyInfo> memberAccesses = new();
		while (true)
		{
			if (current is MemberExpression memberExpression)
			{
				if (memberExpression.Member is not PropertyInfo propertyInfo)
					throw new InvalidOperationException($"Member access {memberExpression.Member.Name} is not a property.");
				memberAccesses.Add(propertyInfo);
				current = memberExpression.Expression!;
				continue;
			}
			
			if (current is not ParameterExpression parameterExpression)
				throw new InvalidOperationException("Only property and parameter access expressions are allowed.");
			break;
		}
		
		if (memberAccesses.Count == 0)
			throw new InvalidOperationException("No path provided.");

		return new PathSubscription<TResult>(memberAccesses, source, onChanged, equalityComparer ?? EqualityComparer<TResult>.Default, valueIfNull);
	}
	
	// TODO: onChanged! Just a notification (allows surfacing exceptions), or just a value change (exceptions ignored)?
	
	private sealed class PathSubscription<T> : IDisposable
	{
		public PathSubscription(IReadOnlyList<PropertyInfo> path, INotifyPropertyChanged initialSource, Action<Exception?, T?> onChange, IEqualityComparer<T> equalityComparer, T? defaultValue)
		{
			_onChange = onChange;
			_defaultValue = defaultValue;
			_equalityComparer = equalityComparer;
			
			_tail = new(path.First(), AfterEvaluate);
			IPathSegmentSubscription current = _tail;
			for (int i = 1; i != path.Count; ++i)
				current = new IntermediatePathSegmentSubscription(path[i], current);
			_head = current;
			
			_head.SetSource(initialSource);
		}

		public void Dispose() => _head.SetSource(null);
		
		private void AfterEvaluate()
		{
			Exception? exception = null;
			T? value = default;
			try
			{
				value = _tail.GetResult(_defaultValue);
			}
			catch (Exception ex)
			{
				exception = ex;
			}
			
			if (exception != null)
			{
				_onChange(exception, default);
			}
			else if (!_equalityComparer.Equals(value, _lastValue))
			{
				_lastValue = value;
				_onChange(null, value);
			}
		}

		private readonly IPathSegmentSubscription _head;
		private readonly TerminalPathSegmentSubscription<T> _tail;
		private readonly Action<Exception?, T?> _onChange;
		private readonly T? _defaultValue;
		private readonly IEqualityComparer<T> _equalityComparer;
		private T? _lastValue;
	}
	
	private interface IPathSegmentSubscription
	{
		void SetSource(object? source);
	}

	private sealed class IntermediatePathSegmentSubscription : IPathSegmentSubscription
	{
		public IntermediatePathSegmentSubscription(PropertyInfo propertyInfo, IPathSegmentSubscription next)
		{
			_propertyInfo = propertyInfo;
			_next = next;
		}

		public void Clear()
		{
			_resultException = null;
			_resultValue = null;
		}

		public void SetSource(object? source)
		{
			if (ReferenceEquals(source, _source))
				return;

			if (_source is INotifyPropertyChanged oldNotifyPropertyChanged)
				oldNotifyPropertyChanged.PropertyChanged -= OnSourcePropertyChanged;

			_source = source;

			if (source is INotifyPropertyChanged notifyPropertyChanged)
				notifyPropertyChanged.PropertyChanged += OnSourcePropertyChanged;

			Evaluate();
		}

		private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == _propertyInfo.Name)
				Evaluate();
		}

		private void Evaluate()
		{
			if (_source == null)
			{
				_resultException = null;
				_resultValue = null;
			}
			else
			{
				try
				{
					_resultException = null;
					_resultValue = _propertyInfo.GetValue(_source);
				}
				catch (Exception ex)
				{
					_resultException = ex;
					_resultValue = null;
				}
			}

			_next.SetSource(_resultValue);
		}

		private readonly PropertyInfo _propertyInfo;
		private readonly IPathSegmentSubscription _next;
		private object? _source;
		private Exception? _resultException;
		private object? _resultValue;
	}

	private sealed class TerminalPathSegmentSubscription<T> : IPathSegmentSubscription
	{
		public TerminalPathSegmentSubscription(PropertyInfo propertyInfo, Action afterEvaluate)
		{
			_propertyInfo = propertyInfo;
			_afterEvaluate = afterEvaluate;
		}
		
		public void SetSource(object? source)
		{
			if (ReferenceEquals(source, _source))
				return;
				
			if (_source is INotifyPropertyChanged oldNotifyPropertyChanged)
				oldNotifyPropertyChanged.PropertyChanged -= OnSourcePropertyChanged;

			_source = source;

			if (source is INotifyPropertyChanged notifyPropertyChanged)
				notifyPropertyChanged.PropertyChanged += OnSourcePropertyChanged;

			Evaluate();
		}
		
		public T? GetResult(T? defaultValue)
		{
			if (_resultException != null)
				ExceptionDispatchInfo.Capture(_resultException).Throw();

			if (_resultValue == null)
				return defaultValue;
			return _resultValue.Value;
		}

		private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == _propertyInfo.Name)
				Evaluate();
		}

		private void Evaluate()
		{
			if (_source == null)
			{
				_resultException = null;
				_resultValue = null;
			}
			else
			{
				try
				{
					_resultException = null;
					var untypedValue = _propertyInfo.GetValue(_source);
					_resultValue = new Result(untypedValue is null ? default : (T?)untypedValue);
				}
				catch (Exception ex)
				{
					_resultException = ex;
					_resultValue = null;
				}
			}
			
			_afterEvaluate();
		}

		private readonly PropertyInfo _propertyInfo;
		private readonly Action _afterEvaluate;
		private object? _source;
		private Exception? _resultException;
		private Result? _resultValue;
		
		private sealed record class Result(T? Value);
	}
}
