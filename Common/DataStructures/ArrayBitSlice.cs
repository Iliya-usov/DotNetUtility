using System.Collections.Generic;
using Common.Extensions;
using JetBrains.Annotations;

namespace Common.DataStructures
{
    public class ArrayBitSlice<T> : BitSliceBase<int[], T> where T : unmanaged
    {
        public ArrayBitSlice(uint byteOffset, [CanBeNull] IEqualityComparer<T>? comparer = null) : base(byteOffset, null, comparer)
        {
        }

        public override unsafe T CompareExchange(ref int[] set, T value, T comparand)
        {
            AssertArrayCapacity(set);

            var oldSet = set;
            fixed (int* ptr = set)
            {
                lock (oldSet)
                {
                    var oldValue = ReadValue((byte*) ptr);
                    if (Comparer.Equals(oldValue, comparand))
                        WriteValue((byte*) ptr, comparand);

                    return oldValue;
                }
            }
        }

        private void AssertArrayCapacity(int[] set) => AssertCapacity(set.Length * sizeof(int));

        public override unsafe T ReadValue(ref int[] set)
        {
            AssertArrayCapacity(set);
            fixed (int* ptr = set) return ReadValue((byte*) ptr);
        }

        public override unsafe void WriteValue(ref int[] set, T value)
        {
            AssertArrayCapacity(set);
            fixed (int* ptr = set) WriteValue((byte*) ptr, value);
        }

        protected override bool AreEquals(int[] left, int[] right) => left.SequentialEqual(right);
    }
}