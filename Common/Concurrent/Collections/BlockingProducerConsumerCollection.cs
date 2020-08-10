using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Common.Exceptions;

namespace Common.Concurrent.Collections
{
  public class BlockingProducerConsumerCollection<T> : IBlockingProducerConsumerCollection<T>
  {
    private IProducerConsumerCollection<T>? myCollection;

    private readonly object myLock = new object();

    private IProducerConsumerCollection<T> CollectionOrThrow
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => myCollection ?? throw new OperationCanceledException("The collection was disposed");
    }
    
    public bool IsAlive => Volatile.Read(ref myCollection) != null;
    
    public int Count
    {
      get
      {
        lock (myLock) return CollectionOrThrow.Count;
      }
    }

    public bool IsSynchronized => true;

    [Obsolete("Do not use", true)]
    public object SyncRoot => throw new NotSupportedException("Sync root is not supported");

    public BlockingProducerConsumerCollection(IProducerConsumerCollection<T> collection)
    {
      myCollection = collection;
    }

    public bool TryAdd(T item)
    {
      lock (myLock)
      {
        if (CollectionOrThrow.TryAdd(item))
        {
          Monitor.Pulse(myLock);
          return true;
        }

        return false;
      }
    }

    public bool TryTake([MaybeNullWhen(false)] out T item)
    {
      lock (myLock) return CollectionOrThrow.TryTake(out item);
    }

    public bool TryTake(int ms, [MaybeNullWhen(false)] out T item)
    {
      lock (myLock)
      {
        if (CollectionOrThrow.TryTake(out item))
          return true;

        if (!Monitor.Wait(myLock, ms))
          return false;

        return CollectionOrThrow.TryTake(out item);
      }
    }

    public T TakeOrBlock(int ms = -1)
    {
      lock (myLock)
      {
        if (CollectionOrThrow.TryTake(out var item))
          return item;

        if (!Monitor.Wait(myLock, ms))
          throw new TimeoutException($"Cannot take element for {ms} milliseconds");

        return CollectionOrThrow.TryTake(out item) ? item : throw new InvalidStateException("The collection must have at least one element");
      }
    }
    
    public void CopyTo(Array array, int index)
    {
      lock (myLock) CollectionOrThrow.CopyTo(array, index);
    }

    public void CopyTo(T[] array, int index)
    {
      lock (myLock) CollectionOrThrow.CopyTo(array, index);
    }

    public T[] ToArray()
    {
      lock (myLock) return CollectionOrThrow.ToArray();
    }
    
    public IEnumerator<T> GetEnumerator()
    {
      var copy = ToArray();
      foreach (var item in copy)
        yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
      lock (myLock)
      {
        if (myCollection == null) return;
        
        try
        {
          DisposeInternalResources();
        }
        finally
        {
          Volatile.Write(ref myCollection, default);
          Monitor.PulseAll(myLock); 
        }
      }
    }
    
    protected virtual void DisposeInternalResources() {}
  }
}