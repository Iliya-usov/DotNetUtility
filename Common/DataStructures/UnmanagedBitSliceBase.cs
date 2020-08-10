using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Common.DataStructures
{
  public abstract class UnmanagedBitSliceBase<TSet, T> : BitSliceBase<TSet, T> where T : unmanaged
  {
    protected UnmanagedBitSliceBase(uint byteOffset, int capacity, [CanBeNull] IEqualityComparer<T>? comparer = null) : base(byteOffset, capacity, comparer)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe T ReadValue(ref TSet set)
    {
      var atomicRead = AtomicRead(ref set);
      return ReadValue(ToBytePtr(ref atomicRead));
    }

    public override T CompareExchange(ref TSet set, T value, T comparand)
    {
      do
      {
        var oldSet = set; // copy
        var oldValue = ReadValue(ref oldSet);

        if (!Comparer.Equals(oldValue, comparand))
          return oldValue;

        var newSet = oldSet;
        WriteValue(ref newSet, value);

        var realOldSet = CompareExchange(ref set, newSet, oldSet);
        if (AreEquals(realOldSet, oldSet)) 
          return ReadValue(ref realOldSet);
        
      } while (true);
    }

    protected abstract TSet CompareExchange(ref TSet set, TSet newValue, TSet comparand);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe void WriteValue(ref TSet set, T value) => WriteValue(ToBytePtr(ref set), value);

    protected abstract TSet AtomicRead(ref TSet set);
    protected abstract unsafe byte* ToBytePtr(ref TSet set);
  }
}