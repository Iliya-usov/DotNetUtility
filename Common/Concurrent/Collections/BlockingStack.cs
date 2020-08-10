using System.Diagnostics.CodeAnalysis;
using Common.Collections;
using Common.Exceptions;

namespace Common.Concurrent.Collections
{
  public sealed class BlockingStack<T> : BlockingProducerConsumerCollection<T>
  {
    public BlockingStack() : base(new ProducerConsumerStack<T>())
    {
    }

    public void Push(T item)
    {
      if (!TryAdd(item))
        throw new InvalidStateException("Element must be added");
    }

    public T PopOrBlock(int ms = -1) => TakeOrBlock(ms);
    
    public bool TryPop([MaybeNullWhen(false)] out T item) => TryTake(out item);
    public bool TryPop(int ms, [MaybeNullWhen(false)] out T item) => TryTake(ms, out item);
  }
}