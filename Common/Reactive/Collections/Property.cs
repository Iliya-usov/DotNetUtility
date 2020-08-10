using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Common.Lifetimes;

namespace Common.Reactive.Collections
{
  public class Property<T> : IMutableProperty<T>
  {
    [AllowNull] private T myValue; 
    private readonly Signal<T> mySignal;
    
    public bool HasValue { get; private set; }

    public ISource<T> Change => mySignal;
    public T ValueOrDefault => myValue;
    public T ValueOrThrow => HasValue ? myValue : throw new InvalidOperationException("Property has not value"); // todo exception?

    public Property(T defaultValue)
    {
      mySignal = new Signal<T>();
      myValue = defaultValue;
      HasValue = true;
    }
    
    public Property()
    {
      mySignal = new Signal<T>();
      myValue = default;
      HasValue = false;
    }

    public void Advice(Lifetime lifetime, Action<T> action)
    {
      bool hasValue;
      T value;
      
      lock (mySignal)
      {
        hasValue = HasValue;
        value = myValue;
        
        mySignal.Advice(lifetime, action);
      }

      if (hasValue) action(value);
    }

    public void SetValue(T value)
    {
      lock (mySignal)
      {
        if (HasValue && EqualityComparer<T>.Default.Equals(myValue, value))
          return;

        myValue = value;
        HasValue = true;
      }

      mySignal.Fire(value);
    }
  }
}