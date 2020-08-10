using System.Diagnostics.CodeAnalysis;
using Common.Collections;
using Common.Exceptions;

namespace Common.Concurrent.Collections
{
  public sealed class BlockingPriorityQueue<T> : BlockingProducerConsumerCollection<ItemWithPriority<T>>
  {
    public BlockingPriorityQueue() : base(new SimplePriorityQueue<T>())
    {
    }
    
    public T DequeueOrBlock(int ms = -1)
    {
      var value = TakeOrBlock(ms);
      return value.Value;
    }

    public void Enqueue(T item, Priority priority)
    {
      if (!TryAdd(new ItemWithPriority<T>(item, priority)))
        throw new InvalidStateException("Element must be added");
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

    public bool TryDequeue(int ms, [MaybeNullWhen(false)] out T item)
    {
      if (TryTake(ms, out var value))
      {
        item = value.Value;
        return true;
      }

      item = default;
      return false;
    }
  }
}