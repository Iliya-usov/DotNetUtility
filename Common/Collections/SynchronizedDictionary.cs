using System;
using System.Collections.Generic;

namespace Common.Collections
{
  public class SynchronizedDictionary<TKey, TValue> where TKey : notnull
  {
    private int myCount;
    private readonly Dictionary<TKey, Entry> myMap;

    public int Count => myCount;

    public SynchronizedDictionary(int capacity = 0, IEqualityComparer<TKey>? comparer = null)
    {
      myMap = new Dictionary<TKey, Entry>(capacity, comparer);
    }

    public void Add(TKey key, TValue value)
    {
      lock (myMap)
      {
        if (myMap.ContainsKey(key))
          throw new ArgumentException($"An element with the same key: {key} already exists in the Dictionary.");
        
        myMap.Add(key, new Entry(value));
        myCount++;
      }
    }
    
    public bool Remove(TKey key)
    {
      lock (myMap)
      {
        if (myMap.Remove(key))
        {
          myCount--;
          return true;
        }

        return false;
      }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
      var entry = GetOrCreateEntry(key, func);

      try
      {
        return entry.GetOrCreateValue(key, func);
      }
      catch
      {
        Remove(key, entry);
        throw;
      }
    }
    
    private Entry GetOrCreateEntry(TKey key, Func<TKey, TValue> func)
    {
      lock (myMap)
      {
        if (myMap.TryGetValue(key, out var value))
          return value;

        value = new Entry(func);
        myMap[key] = value;
        myCount++;
        return value;
      }
    }

    private bool Remove(TKey key, Entry entry)
    {
      lock (myMap)
      {
        if (myMap.TryGetValue(key, out var value) && value == entry && myMap.Remove(key))
        {
          myCount--;
          return true;
        }

        return false;
      }
    }
    
    private class Entry
    {
      private volatile Func<TKey, TValue>? myFactory;

      public TValue Result { get; private set; }
      public bool HasValue => myFactory == null;

      public Entry(Func<TKey, TValue> factory)
      {
        myFactory = factory;
        Result = default!;
      }
      
      public Entry(TValue value)
      {
        myFactory = null;
        Result = value;
      }
      
      public TValue GetOrCreateValue(TKey key, Func<TKey, TValue> fallback)
      {
        if (HasValue) return Result;

        lock (this)
        {
          if (HasValue) return Result;
          
          Result = Calc(key, fallback);
          myFactory = null;

          return Result;
        }
      }

      private TValue Calc(TKey key, Func<TKey, TValue> fallback)
      {
        try
        {
          return myFactory!(key);
        }
        catch
        {
          if (fallback == myFactory)
            throw;

          return fallback(key);
        }
      }
    }
  }
}