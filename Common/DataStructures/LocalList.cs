using System;
using System.Runtime.CompilerServices;

namespace Common.DataStructures
{
  // todo 
  public struct LocalList<T>
  {
    private T myItem0;
    private T myItem1;
    private T myItem2;
    private T myItem3;
    private T[] myArray;

    public int Count { get; private set; }

    public void Add(T value)
    {
      switch (Count)
      {
        case 0: myItem0 = value; break;
        case 1: myItem1 = value; break;
        case 2: myItem2 = value; break;
        case 3: myItem3 = value; break;

        default:
          myArray ??= new T[4];
          var index = Count - 4;
          if (myArray.Length <= index)
            Array.Resize(ref myArray, index * 2);
          myArray[index] = value;
          break;
      }

      Count++;
    }

    public T this[int index]
    {
      get
      {
        CheckIndex(index);
        return index switch
        {
          0 => myItem0,
          1 => myItem1,
          2 => myItem2,
          3 => myItem3,
          _ => myArray[index - 4]
        };
      }
      set
      {
        CheckIndex(index);
        switch (index)
        {
          case 0: myItem0 = value; break;
          case 1: myItem1 = value; break;
          case 2: myItem2 = value; break;
          case 3: myItem3 = value; break;
          default:
            myArray ??= new T[4];
            myArray[index - 1] = value;
            break;
        }
      }
    }

    public T Pop()
    {
      switch (--Count)
      {
        case 0:
          var item0 = myItem0;
          myItem0 = default!;
          return item0;
        case 1:
          var item1 = myItem1;
          myItem1 = default!;
          return item1;
        case 2:
          var item2 = myItem2;
          myItem2 = default!;
          return item2;
        case 3:
          var item3 = myItem3;
          myItem3 = default!;
          return item3;
        default:
          var index = Count - 3;
          var value = myArray[index];
          myArray[index] = default!;
          return value;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckIndex(int index)
    {
      if (index < 0 || index >= Count) 
        throw new ArgumentOutOfRangeException($"{nameof(index)} = {index}, but {nameof(Count)} = {Count}");
    }
  }
}