using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Common.DataStructures
{
  // todo support bit (bool)
  public abstract class BitSliceBase<TSet, T> where T : unmanaged
  {
    protected readonly IEqualityComparer<T> Comparer;

    protected uint ByteOffset { get; }

    protected BitSliceBase(uint byteOffset, int? capacity, IEqualityComparer<T>? comparer = null)
    {
      ByteOffset = byteOffset;
      AssertCapacity(capacity);

      Comparer = comparer ?? EqualityComparer<T>.Default;
    }

    protected unsafe void AssertCapacity(int? capacity)
    {
      if (capacity.HasValue && ByteOffset + sizeof(T) > capacity)
        throw new ArgumentException("HUIta");
    }

    public abstract T CompareExchange(ref TSet set, T value, T comparand);

    public bool TryUpdate(ref TSet set, T newValue)
    {
      var oldValue = ReadValue(ref set);
      return TryUpdate(ref set, newValue, oldValue);
    }

    public bool TryUpdate(ref TSet set, T newValue, T oldValue)
    {
      var realOldValue = CompareExchange(ref set, newValue, oldValue);
      return Comparer.Equals(oldValue, realOldValue);
    }

    public void Update(ref TSet set, T newValue)
    {
      while (!TryUpdate(ref set, newValue)) { /*nothing*/ }
    }

    public T Increment(ref TSet set)
    {
      do
      {
        var value = ReadValue(ref set);
        var newValue = UnmanagedArithmetics<T>.Default.Increment(value);
        if (TryUpdate(ref set, newValue, value))
          return newValue;
        
      } while (true);
    }
    
    public bool TryIncrement(ref TSet set, Func<TSet, bool> checkForInterrupt,out T updatedValue)
    {
      do
      {
        var value = ReadValue(ref set);
        var newValue = UnmanagedArithmetics<T>.Default.Increment(value);
        if (TryUpdate(ref set, newValue, value))
        {
          updatedValue = newValue;
          return true;
        }

        if (checkForInterrupt(set))
        {
          updatedValue = default;
          return false;
        }

      } while (true);
    }
    
    public T Decrement(ref TSet set)
    {
      do
      {
        var oldValue = ReadValue(ref set);
        var newValue = UnmanagedArithmetics<T>.Default.Decrement(oldValue);
        if (TryUpdate(ref set, newValue, oldValue))
          return newValue;
        
      } while (true);
    }
    
    public abstract T ReadValue(ref TSet set);
    public abstract void WriteValue(ref TSet set, T value);
    protected abstract bool AreEquals(TSet left, TSet right);

    protected unsafe T ReadValue(byte* ptr) => Unsafe.Read<T>(ptr + ByteOffset);
    protected unsafe void WriteValue(byte* ptr, T value) => Unsafe.Write(ptr + ByteOffset, value);
  }
}