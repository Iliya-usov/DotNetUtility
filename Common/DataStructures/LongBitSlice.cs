using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Common.DataStructures
{
  public class LongBitSlice<T> : UnmanagedBitSliceBase<long, T> where T : unmanaged
  {
    public LongBitSlice(uint byteOffset = 0, IEqualityComparer<T>? comparer = null) : base(byteOffset, sizeof(long), comparer) { }

    public unsafe IntBitSlice<TNext> Next<TNext>() where TNext : unmanaged
    {
      return new IntBitSlice<TNext>((uint) (ByteOffset + sizeof(T)));
    }

    protected override unsafe byte* ToBytePtr(ref long set) => (byte*) Unsafe.AsPointer(ref set);
    protected override long AtomicRead(ref long set) => Interlocked.Read(ref set);

    protected override long CompareExchange(ref long set, long newValue, long comparand)
    {
      return Interlocked.CompareExchange(ref set, newValue, comparand);
    }

    protected override bool AreEquals(long left, long right) => left == right;
  }
}