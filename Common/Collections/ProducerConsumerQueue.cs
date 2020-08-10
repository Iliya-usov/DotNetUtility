using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Common.Collections
{
  public sealed class ProducerConsumerQueue<T> : IProducerConsumerCollection<T>
  {
    private readonly Queue<T> myQueue;
    
    public ProducerConsumerQueue(Queue<T>? queue = null) => myQueue = queue ?? new Queue<T>();

    public int Count => myQueue.Count;
    public bool IsSynchronized => false;
    public object SyncRoot => throw new NotSupportedException();
    
    public void CopyTo(T[] array, int index) => myQueue.CopyTo(array, index);

    public void CopyTo(Array array, int index)
    {
      var source = ToArray();
      Array.Copy(source, 0, array, index, source.Length);
    }
    
    public bool TryTake([MaybeNullWhen(false)] out T item) => myQueue.TryDequeue(out item);
    
    public bool TryAdd(T item)
    {
      myQueue.Enqueue(item);
      return true;
    }
    
    public T[] ToArray() => myQueue.ToArray();
    
    public IEnumerator<T> GetEnumerator() => myQueue.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => myQueue.GetEnumerator();
  }
}