using System;
using System.Collections.Immutable;
using Common.Lifetimes;
using Common.Logging;

namespace Common.Reactive.Collections
{
  public class Signal<T> : ISignal<T>
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<Signal<T>>();
    
    // todo maybe optimize
    private ImmutableArray<Lifetimed<Action<T>>> myListeners = ImmutableArray<Lifetimed<Action<T>>>.Empty;
    
    public void Advice(Lifetime lifetime, Action<T> action)
    {
      if (lifetime.IsNotAlive) return;

      lifetime.TryBracket(() =>
      {
        var lifetimed = action.ToLifetimed(lifetime);
        lock (this) myListeners = myListeners.Add(lifetimed);
        return lifetimed;
      }, lifetimed =>
      {
        lock (this) myListeners = myListeners.Remove(lifetimed);
      });
    }

    public void Fire(T value)
    {
      // ReSharper disable once InconsistentlySynchronizedField
      foreach (var lifetimed in myListeners)
      {
        if (lifetimed.Lifetime.IsNotAlive) return;
        
        try
        {
          lifetimed.Value(value);
        }
        catch (Exception e)
        {
          ourLogger.Error(e, "Error in signal handler"); // todo text
        }
      }
    }
  }
}