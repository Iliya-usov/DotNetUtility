using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Common.Collections
{
  public sealed class ProducerConsumerStack<T> : IProducerConsumerCollection<T>
  {
    private readonly Stack<T> myStack;
    
    public ProducerConsumerStack(Stack<T>? stack = null) => myStack = stack ?? new Stack<T>();

    public int Count => myStack.Count;
    public bool IsSynchronized => false;
    public object SyncRoot => throw new NotSupportedException();
    
    public void CopyTo(T[] array, int index) => myStack.CopyTo(array, index);

    public void CopyTo(Array array, int index)
    {
      var source = ToArray();
      Array.Copy(source, 0, array, index, source.Length);
    }
    
    public bool TryTake([MaybeNullWhen(false)] out T item) => myStack.TryPop(out item);
    
    public bool TryAdd(T item)
    {
      myStack.Push(item);
      return true;
    }
    
    public T[] ToArray() => myStack.ToArray();
    
    public IEnumerator<T> GetEnumerator() => myStack.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => myStack.GetEnumerator();
  }
}