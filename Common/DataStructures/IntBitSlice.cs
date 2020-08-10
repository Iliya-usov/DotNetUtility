using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Common.DataStructures
{
  public class IntBitSlice<T> : UnmanagedBitSliceBase<int, T> where T : unmanaged
  {
    public IntBitSlice(uint byteOffset = 0, IEqualityComparer<T>? comparer = null) : base(byteOffset, sizeof(int), comparer) { }

    public unsafe IntBitSlice<TNext> Next<TNext>() where TNext : unmanaged
    {
      return new IntBitSlice<TNext>((uint) (ByteOffset + sizeof(T)));
    }

    protected override unsafe byte* ToBytePtr(ref int set) => (byte*) Unsafe.AsPointer(ref set);
    protected override int AtomicRead(ref int set) => set;

    protected override int CompareExchange(ref int set, int newValue, int comparand)
    {
      return Interlocked.CompareExchange(ref set, newValue, comparand);
    }

    protected override bool AreEquals(int left, int right) => left == right;
  }
}