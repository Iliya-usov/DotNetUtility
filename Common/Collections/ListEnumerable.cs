using System.Collections.Generic;
using JetBrains.Annotations;

namespace Common.Collections
{
  public readonly struct ListEnumerable<T>
  {
    private readonly IList<T> myList;
    public ListEnumerable(IList<T> list)
    {
      myList = list;
    }
    
    [UsedImplicitly]
    public Enumerator GetEnumerator() => new Enumerator(myList);
    
    public struct Enumerator
    {
      private readonly IList<T> myList;
      private int myIndex;

      [UsedImplicitly]
      public T Current => myList[myIndex];

      public Enumerator(IList<T> list)
      {
        myList = list;
        myIndex = -1;
      }

      [UsedImplicitly]
      public bool MoveNext() => ++myIndex < myList.Count;
    }
  }
}