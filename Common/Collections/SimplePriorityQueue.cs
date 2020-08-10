using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Common.Collections
{
  public enum Priority
  {
    Lowest = 0,
    BelowNormal = 1,
    Normal = 2,
    AboveNormal = 3,
    Highest = 4,
  }

  public readonly struct ItemWithPriority<T>
  {
    public T Value { get; }
    public Priority Priority { get;}

    public ItemWithPriority(T value, Priority priority)
    {
      Value = value;
      Priority = priority;
    }
  }
  
  public class SimplePriorityQueue<T> : IPriorityQueue<T>, IProducerConsumerCollection<ItemWithPriority<T>>
  {
    private readonly Queue<T>[] myBuckets;
    public int Count { get; private set; }

    public bool IsSynchronized => false;
    public object SyncRoot => throw new NotSupportedException();

    public SimplePriorityQueue()
    {
      myBuckets = new Queue<T>[5];
    }
    
    public void Enqueue(T item, Priority priority)
    {
      var bucket = myBuckets[(int) priority];
      bucket.Enqueue(item);
      Count++;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
      if (TryTake(out var value))
      {
        item = value.Value;
        return true;
      }

      item = default;
      return false;
    }

    public T Dequeue()
    {
      if (TryDequeue(out var item))
        return item;

      throw new ArgumentOutOfRangeException("Queue is empty");
    }

    public bool TryAdd(ItemWithPriority<T> item)
    {
      Enqueue(item.Value, item.Priority);
      return true;
    }

    public bool TryTake([MaybeNullWhen(false)] out ItemWithPriority<T> item)
    {
      for (var index = 0; index < myBuckets.Length; index++)
      {
        var bucket = myBuckets[index];
        if (bucket.TryDequeue(out var value))
        {
          Count--;
          item = new ItemWithPriority<T>(value, (Priority) index);
          return true;
        }
      }

      item = default;
      return false;
    }

    public void CopyTo(Array array, int index)
    {
      var source = ToArray();
      Array.Copy(source, 0, array, index, source.Length);
    }
    
    public void CopyTo(ItemWithPriority<T>[] array, int index)
    {
      var count = index;
      foreach (var item in this) 
        array[count++] = item;
    }

    public ItemWithPriority<T>[] ToArray() => Enumerable.ToArray(this);

    public IEnumerator<ItemWithPriority<T>> GetEnumerator()
    {
      for (var index = 0; index < myBuckets.Length; index++)
      {
        var bucket = myBuckets[index];
        var priority = (Priority) index;
        foreach (var item in bucket)
          yield return new ItemWithPriority<T>(item, priority);
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}