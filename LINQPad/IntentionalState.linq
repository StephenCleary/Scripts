<Query Kind="Statements">
  <NuGetReference>Nito.Mvvm.Core</NuGetReference>
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <Namespace>Nito.Mvvm</Namespace>
  <Namespace>System.ComponentModel</Namespace>
</Query>

/*
"Immutable state" is:
- An immutable value type that implements structural equality (e.g., readonly record struct - without mutable members).
- An immutable reference type that implements structural equality (e.g., record class - without mutable members).
- An immutable collection where all generic parameters are immutable types that implements structural equality.
  - Note: System.Collections.Immutable do not implement structural equality, so they need wrappers that implement structural equality.
*/

public readonly record struct StateChangedEventArgs<TImmutableState>(TImmutableState OldState, TImmutableState NewState);

// Note: This is almost-but-not-quite a generic Redux-style store. It differs from a standard store in two aspects:
// 1. For simplicity, this type does not use middleware.
// 2. This type has MVVM-specific code by using PropertyChangedNotificationManager directly.
// A More Proper Design (tm) would support middleware instead and move the PCNM usage into a middleware component.
// Right now, that would just increase complexity, but it is something we can move to in the future.
public sealed class Store<TImmutableState> : IStore<TImmutableState>
{
	private static readonly IEqualityComparer<TImmutableState> _comparer = EqualityComparer<TImmutableState>.Default;
	private readonly Queue<Func<TImmutableState, TImmutableState>> _transforms = new();

	public Store(TImmutableState initialState)
	{
		State = initialState;
	}

	public TImmutableState State { get; private set; }
	public event Action<StateChangedEventArgs<TImmutableState>> StateChanged;

	public void Dispatch(Func<TImmutableState, TImmutableState> transform)
	{
		var executeNow = _transforms.Count == 0;
		_transforms.Enqueue(transform);
		if (!executeNow)
			return;

		using var notify = PropertyChangedNotificationManager.Instance.DeferNotifications();
		while (_transforms.Count != 0)
		{
			var t = _transforms.Dequeue();
			var oldState = State;
			var stateChangedEvent = StateChanged;
			var newState = t(oldState);
			if (!_comparer.Equals(oldState, newState))
			{
				State = newState;
				stateChangedEvent?.Invoke(new(oldState, newState));
			}
		}
	}
	
	static Store()
	{
		ImmutableUtility.ValidateType(typeof(TImmutableState));
	}
}

public interface IState<TImmutableState>
{
	TImmutableState State { get; }
	event Action<StateChangedEventArgs<TImmutableState>> StateChanged;
}

public interface IDispatch<TImmutableState>
{
	void Dispatch(Func<TImmutableState, TImmutableState> transform);
}

public interface IStore<TImmutableState> : IState<TImmutableState>, IDispatch<TImmutableState>
{
}

public sealed class ProjectedState<TSourceImmutableState, TResultImmutableState> : IState<TResultImmutableState>, IDisposable
{
	private static readonly IEqualityComparer<TResultImmutableState> _comparer = EqualityComparer<TResultImmutableState>.Default;

	private readonly IState<TSourceImmutableState> _source;
	private readonly Func<TSourceImmutableState, TResultImmutableState> _project;
	public ProjectedState(IState<TSourceImmutableState> source, Func<TSourceImmutableState, TResultImmutableState> project)
	{
		(_source, _project, State) = (source, project, project(source.State));
		_source.StateChanged += SourceStateChanged;
	}
	public TResultImmutableState State { get; private set; }
	public event Action<StateChangedEventArgs<TResultImmutableState>> StateChanged;
	public void Dispose() => _source.StateChanged -= SourceStateChanged;
	private void SourceStateChanged(StateChangedEventArgs<TSourceImmutableState> args)
	{
		var oldState = State;
		var stateChangedEvent = StateChanged;
		var newState = _project(args.NewState);
		if (!_comparer.Equals(oldState, newState))
		{
			State = newState;
			stateChangedEvent?.Invoke(new(oldState, newState));
		}
	}
}

// VM-style projection that is not aware of Dispatch. So: intents go straight from VM to store.
public sealed class Properties<TImmutableState> : IDisposable
{
	private readonly IRaisePropertyChanged _owner;
	private readonly IState<TImmutableState> _state;
	
	public Properties(IRaisePropertyChanged owner, IState<TImmutableState> state)
	{
		_owner = owner;
		_state = state;
		_state.StateChanged += OnStateChanged;
	}

	public void Dispose() => _state.StateChanged -= OnStateChanged;

	public TProperty Property<TProperty>(Func<TImmutableState, TProperty> implementation, IEqualityComparer<TProperty> comparer = null, [CallerMemberName] string propertyName = "")
	{
		if (!_properties.TryGetValue(propertyName, out var property))
			_properties[propertyName] = property = new SimplePropertyImplementation<TProperty>(implementation, comparer, _state.State);
		return ((SimplePropertyImplementation<TProperty>)property).Value;
	}

	private void OnStateChanged(StateChangedEventArgs<TImmutableState> args)
	{
		foreach (var (propertyName, property) in _properties)
		{
			var propertyChanged = property.OnStateChanged(args.NewState);
			if (propertyChanged)
				PropertyChangedNotificationManager.Instance.Register(_owner, propertyName);
		}
	}

	private readonly Dictionary<string, IPropertyImplementation> _properties = new();
	private interface IPropertyImplementation
	{
		bool OnStateChanged(TImmutableState newState);
	}
	private sealed class SimplePropertyImplementation<TProperty> : IPropertyImplementation
	{
		public SimplePropertyImplementation(Func<TImmutableState, TProperty> implementation,
			IEqualityComparer<TProperty> comparer,
			TImmutableState state)
		{
			_implementation = implementation;
			_comparer = comparer ?? EqualityComparer<TProperty>.Default;
			Value = implementation(state);
		}

		public bool OnStateChanged(TImmutableState newState)
		{
			var oldValue = Value;
			Value = _implementation(newState);
			return !_comparer.Equals(oldValue, Value);
		}

		public TProperty Value { get; private set; }

		private readonly Func<TImmutableState, TProperty> _implementation;
		private readonly IEqualityComparer<TProperty> _comparer;
	}
}

public sealed class Intents<TImmutableState>
{
	private readonly IDispatch<TImmutableState> _dispatch;
	public Intents(IDispatch<TImmutableState> dispatch)
	{
		_dispatch = dispatch;
	}
	
	public void Intent(Func<TImmutableState, TImmutableState> implementation, [CallerMemberName] string intentName = "")
	{
		if (!_intents.TryGetValue(intentName, out var intent))
			_intents[intentName] = intent = implementation;
		_dispatch.Dispatch(intent);
	}
	
	private readonly Dictionary<string, Func<TImmutableState, TImmutableState>> _intents = new();
}

// TODO: is this necessary for v1?
public sealed class Atom<TDispatchImmutableState, TImmutableState>
{
	private readonly Properties<TImmutableState> _properties;
	private readonly Intents<TDispatchImmutableState> _intents;
	
	public Atom(IRaisePropertyChanged owner, IState<TImmutableState> state, IDispatch<TDispatchImmutableState> dispatch)
	{
		_properties = new(owner, state);
		_intents = new(dispatch);
	}

	public TProperty Property<TProperty>(Func<TImmutableState, TProperty> implementation, IEqualityComparer<TProperty> comparer = null, [CallerMemberName] string propertyName = "")
		=> _properties.Property(implementation, comparer, propertyName);
	public void Intent(Func<TDispatchImmutableState, TDispatchImmutableState> implementation, [CallerMemberName] string intentName = "")
		=> _intents.Intent(implementation, intentName);
}

public sealed class Atom<TImmutableState>
{
	private readonly Properties<TImmutableState> _properties;
	private readonly Intents<TImmutableState> _intents;

	public Atom(IRaisePropertyChanged owner, IStore<TImmutableState> store)
	{
		_properties = new(owner, store);
		_intents = new(store);
	}

	public TProperty Property<TProperty>(Func<TImmutableState, TProperty> implementation, IEqualityComparer<TProperty> comparer = null, [CallerMemberName] string propertyName = "")
		=> _properties.Property(implementation, comparer, propertyName);
	public void Intent(Func<TImmutableState, TImmutableState> implementation, [CallerMemberName] string intentName = "")
		=> _intents.Intent(implementation, intentName);
}

public readonly record struct MyState(int Counter);
public readonly record struct CustomState(int ScaledCounter);

public sealed class ViewModel: IRaisePropertyChanged
{
	private readonly ProjectedState<MyState, CustomState> _scaleProjection;
	
	private readonly Atom<MyState, CustomState> _atom;
	
	public ViewModel(Store<MyState> store)
	{
		_scaleProjection = new(store, x => new(x.Counter * 2));
		_atom = new(this, _scaleProjection, store);
	}
	
	public int Count
	{
		get => _atom.Property(x => x.ScaledCounter);
		set => _atom.Intent(_ => new(value / 2));
	}
	
	public void Increment() => _atom.Intent(x => new(x.Counter + 1));
	public void Decrement() => _atom.Intent(x => new(x.Counter - 1));

	public void RaisePropertyChanged(PropertyChangedEventArgs args)
	{
		throw new NotImplementedException();
	}
}

public sealed class ViewModel2 : IRaisePropertyChanged
{
	private readonly Atom<MyState> _atom;

	public ViewModel2(Store<MyState> store)
	{
		_atom = new(this, store);
	}

	public int Count
	{
		get => _atom.Property(x => x.Counter);
		set => _atom.Intent(_ => new(value));
	}

	public void Increment() => _atom.Intent(x => new(x.Counter + 1));
	public void Decrement() => _atom.Intent(x => new(x.Counter - 1));

	public void RaisePropertyChanged(PropertyChangedEventArgs args)
	{
		throw new NotImplementedException();
	}
}

public static class ImmutableUtility
{
	// Attempts to catch the most common mistakes when defining immutable state types.
	public static void ValidateType(Type type)
	{
		if (type.IsPrimitive || type == typeof(string))
			return;

		if (type.GetInterface("IEquatable`1") == null)
			throw new InvalidOperationException($"Type {type.FullName} does not support structural equality.");

		// All collection types must have their generic parameter types be immutable.
		if (type.GetInterface("IEnumerable`1") != null)
		{
			foreach (var genericType in type.GenericTypeArguments)
				ValidateType(genericType);
		}
		else
		{
			// Non-collection types must:
			// - Not have any non-readonly methods (including property setters).
			// - Not have any non-readonly/non-compiler-generated fields.
			// - Have all their field types be immutable types.

			var properties = type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(x => x.DeclaringType != typeof(object) && x.DeclaringType != typeof(ValueType));
			var methods = type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(x => x.DeclaringType != typeof(object) && x.DeclaringType != typeof(ValueType));
			var fields = type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(x => x.DeclaringType != typeof(object) && x.DeclaringType != typeof(ValueType));

			// User-defined methods must be readonly.
			foreach (var method in methods)
			{
				if (method.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null)
					continue;

				if (method.GetCustomAttribute(typeof(IsReadOnlyAttribute)) == null)
					throw new InvalidOperationException($"Method {method.DeclaringType?.FullName}.{method.Name} is not readonly.");
			}

			// Property setters must be init-only.
			foreach (var prop in properties)
			{
				var setter = prop.SetMethod;
				if (setter == null)
					continue;
				var isInitOnly = setter.ReturnParameter?.GetRequiredCustomModifiers()?.Contains(typeof(IsExternalInit)) ?? false;
				if (!isInitOnly)
					throw new InvalidOperationException($"Property {prop.DeclaringType?.FullName}.{prop.Name} setter is not init-only.");
			}

			// User-defined fields must be readonly.
			foreach (var field in fields)
			{
				if (field.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null)
					continue;

				if (field.GetCustomAttribute(typeof(IsReadOnlyAttribute)) == null)
					throw new InvalidOperationException($"Field {field.DeclaringType?.FullName}.{field.Name} is not readonly.");
			}

			// Field values must be immutable.
			foreach (var fieldType in fields.Select(x => x.FieldType))
				ValidateType(fieldType);
		}
	}
}