using System.Diagnostics.CodeAnalysis;

namespace Common.Collections
{
  public interface IPriorityQueue<T>
  {
    void Enqueue(T item, Priority priority);
    bool TryDequeue([MaybeNullWhen(false)] out T item);

    T Dequeue();
  }
}