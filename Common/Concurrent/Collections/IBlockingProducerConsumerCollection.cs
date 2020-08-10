using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Common.Concurrent.Collections
{
  public interface IBlockingProducerConsumerCollection<T> : IProducerConsumerCollection<T>, IDisposable
  {
    bool IsAlive { get; }
    
    bool TryTake(int ms, [MaybeNullWhen(false)] out T item);
    T TakeOrBlock(int ms = -1);
    
    [Obsolete("Do not use", true)] new object SyncRoot { get; }
  }
}