using System.Collections.Generic;
using JetBrains.Annotations;

namespace Common.Collections
{
  public readonly struct ReadonlyListEnumerable<T>
  {
    private readonly IReadOnlyList<T> myList;
    public ReadonlyListEnumerable(IReadOnlyList<T> list)
    {
      myList = list;
    }
    
    [UsedImplicitly]
    public Enumerator GetEnumerator() => new Enumerator(myList);
    
    public struct Enumerator
    {
      private readonly IReadOnlyList<T> myList;
      private int myIndex;

      [UsedImplicitly]
      public T Current => myList[myIndex];

      public Enumerator(IReadOnlyList<T> list)
      {
        myList = list;
        myIndex = -1;
      }

      [UsedImplicitly]
      public bool MoveNext() => ++myIndex < myList.Count;
    }
  }
}