using System.Diagnostics.CodeAnalysis;
using Common.Collections;
using Common.Exceptions;

namespace Common.Concurrent.Collections
{
  public sealed class BlockingQueue<T> : BlockingProducerConsumerCollection<T>
  {
    public BlockingQueue() : base(new ProducerConsumerQueue<T>())
    {
    }

    public void Enqueue(T item)
    {
      if (!TryAdd(item))
        throw new InvalidStateException("Element must be added");
    }

    public T DequeueOrBlock(int ms = -1) => TakeOrBlock(ms);
    
    public bool TryDequeue([MaybeNullWhen(false)] out T item) => TryTake(out item);
    public bool TryDequeue(int ms, [MaybeNullWhen(false)] out T item) => TryTake(ms, out item);
  }
}