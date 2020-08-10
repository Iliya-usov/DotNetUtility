using System;
using System.Collections.Generic;
using System.Linq;
using Common.Collections;
using Common.Extensions;
using Common.Lifetimes;
using Common.Logging;

namespace Common.Reactive.Collections
{
  public interface IViewableMap<TKey, TValue> where TKey : notnull
  {
    int Length { get; }

    ISource<MapEvent<TKey, TValue>> Change { get; }
    void Advice(Lifetime lifetime, Action<MapEvent<TKey, TValue>> action);
    
    TValue this[TKey key] { get; }
  }
  
  public class ViewableMap<TKey, TValue> : IMutableViewableMap<TKey, TValue> where TKey : notnull
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<ViewableMap<TKey, TValue>>();

    private readonly WeakReference<KeyValuePair<TKey, TValue>[]?> mySnapshot;
    
    private readonly Dictionary<TKey, TValue> myMap;
    private readonly Signal<MapEvent<TKey, TValue>> mySignal;

    public ISource<MapEvent<TKey, TValue>> Change => mySignal;
    public int Length => myMap.Count;
    
    public ViewableMap()
    {
      mySignal = new Signal<MapEvent<TKey, TValue>>();
      myMap = new Dictionary<TKey, TValue>();
      mySnapshot = new WeakReference<KeyValuePair<TKey, TValue>[]?>(null);
    }
    
    public void Advice(Lifetime lifetime, Action<MapEvent<TKey, TValue>> action)
    {
      var snapshot = EmptyArray<KeyValuePair<TKey, TValue>>.Instance;

      lock (mySignal)
      {
        if (mySnapshot.TryGetTarget(out var target))
          snapshot = target;
        else if (!myMap.IsEmpty())
        {
          snapshot = myMap.ToArray();
          mySnapshot.SetTarget(snapshot);
        }

        mySignal.Advice(lifetime, action);
      }

      foreach (var (key, value) in snapshot)
      {
        try
        {
          action(MapEvent.Add(key, value));
        }
        catch (Exception e)
        {
          ourLogger.Error(e, "Failed to call action"); // todo text
        }
      }
    }

    public void Add(TKey key, TValue value)
    {
      lock (mySignal)
      {
        myMap.Add(key, value);
        mySnapshot.SetTarget(null);
      }

      mySignal.Fire(MapEvent.Add(key, value));
    }

    public bool Remove(TKey key)
    {
      throw new System.NotImplementedException();
    }

    public TValue this[TKey key]
    {
      get => throw new System.NotImplementedException();
      set => throw new System.NotImplementedException();
    }
  }
}