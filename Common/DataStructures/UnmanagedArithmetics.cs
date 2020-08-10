using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Common.DataStructures
{
  public interface IUnmanagedArithmetics
  {
  }

  public abstract class UnmanagedArithmetics<T> : IUnmanagedArithmetics where T : unmanaged
  {
    private static UnmanagedArithmetics<T>? ourInstance;

    public static UnmanagedArithmetics<T> Default => ourInstance ??= (UnmanagedArithmetics<T>) GetArithmetics();

    public abstract T Increment(T value);
    public abstract T Decrement(T value);

    private static IUnmanagedArithmetics GetArithmetics()
    {
      if (typeof(T) == typeof(byte)) return ByteArithmetics.Instance;
      if (typeof(T) == typeof(sbyte)) return SByteArithmetics.Instance;
      if (typeof(T) == typeof(short)) return ShortArithmetics.Instance;
      if (typeof(T) == typeof(ushort)) return UShortArithmetics.Instance;
      if (typeof(T) == typeof(int)) return IntArithmetics.Instance;
      if (typeof(T) == typeof(uint)) return UIntArithmetics.Instance;
      if (typeof(T) == typeof(long)) return LongArithmetics.Instance;
      if (typeof(T) == typeof(ulong)) return ULongArithmetics.Instance;

      return typeof(T).IsEnum ? EnumArithmetics<T>.Default : BigIntegerArithmetics<T>.Instance;
    }
  }

  public class ByteArithmetics : UnmanagedArithmetics<byte>
  {
    public static ByteArithmetics Instance { get; } = new ByteArithmetics();

    public override byte Increment(byte value) => checked(++value);
    public override byte Decrement(byte value) => checked(--value);
  }
  
  public class SByteArithmetics : UnmanagedArithmetics<sbyte>
  {
    public static SByteArithmetics Instance { get; } = new SByteArithmetics();

    public override sbyte Increment(sbyte value) => checked(++value);
    public override sbyte Decrement(sbyte value) => checked(--value);
  }

  public class ShortArithmetics : UnmanagedArithmetics<short>
  {
    public static ShortArithmetics Instance { get; } = new ShortArithmetics();
    
    public override short Increment(short value) => checked(++value);
    public override short Decrement(short value) => checked(--value);
  }
  
  public class UShortArithmetics : UnmanagedArithmetics<ushort>
  {
    public static UShortArithmetics Instance { get; } = new UShortArithmetics();

    public override ushort Increment(ushort value) => checked(++value);
    public override ushort Decrement(ushort value) => checked(--value);
  }

  public class IntArithmetics : UnmanagedArithmetics<int>
  {
    public static IntArithmetics Instance { get; } = new IntArithmetics();

    public override int Increment(int value) => checked(++value);
    public override int Decrement(int value) => checked(--value);
  }
  
  public class UIntArithmetics : UnmanagedArithmetics<uint>
  {
    public static UIntArithmetics Instance { get; } = new UIntArithmetics();

    public override uint Increment(uint value) => checked(++value);
    public override uint Decrement(uint value) => checked(--value);
  }
  
  public class LongArithmetics : UnmanagedArithmetics<long>
  {
    public static LongArithmetics Instance { get; } = new LongArithmetics();

    public override long Increment(long value) => checked(++value);
    public override long Decrement(long value) => checked(--value);
  }
  
  public class ULongArithmetics : UnmanagedArithmetics<ulong>
  {
    public static ULongArithmetics Instance { get; } = new ULongArithmetics();

    public override ulong Increment(ulong value) => checked(++value);
    public override ulong Decrement(ulong value) => checked(--value);
  }

  public abstract class EnumArithmetics<T> where T : unmanaged
  {    
    private static UnmanagedArithmetics<T>? ourInstance;

    public static UnmanagedArithmetics<T> Default => ourInstance ??= GetArithmetics();
    
    private static unsafe UnmanagedArithmetics<T> GetArithmetics()
    {
      if (!typeof(T).IsEnum) throw new ArgumentException($"Type: {typeof(T).Name} is not Enum");

      var fieldInfos = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
      foreach (var value in fieldInfos)
      {
        if (value.Name != "value__") continue;
        
        var type = value.FieldType;
        if (type == null) continue;
        
        if (type == typeof(byte)) return EnumArithmetics<T, byte>.Instance;
        if (type == typeof(sbyte)) return EnumArithmetics<T, sbyte>.Instance;
        if (type == typeof(short)) return EnumArithmetics<T, short>.Instance;
        if (type == typeof(ushort)) return EnumArithmetics<T, ushort>.Instance;
        if (type == typeof(int)) return EnumArithmetics<T, int>.Instance;
        if (type == typeof(uint)) return EnumArithmetics<T, uint>.Instance;
        if (type == typeof(long)) return EnumArithmetics<T, long>.Instance;
        if (type == typeof(ulong)) return EnumArithmetics<T, ulong>.Instance;
      }

      return sizeof(T) switch
      {
        sizeof(byte) => EnumArithmetics<T, byte>.Instance,
        sizeof(ushort) => EnumArithmetics<T, short>.Instance,
        sizeof(uint) => EnumArithmetics<T, uint>.Instance,
        sizeof(ulong) => EnumArithmetics<T, ulong>.Instance,
        _ => BigIntegerArithmetics<T>.Instance
      };
    }
  }
  
  public class EnumArithmetics<T, TBase> : UnmanagedArithmetics<T>
    where T : unmanaged
    where TBase : unmanaged
  {  
    public static EnumArithmetics<T, TBase> Instance { get; } = new EnumArithmetics<T, TBase>();

    public override T Increment(T value) => FromBase(UnmanagedArithmetics<TBase>.Default.Increment(ToBase(value)));
    public override T Decrement(T value) => FromBase(UnmanagedArithmetics<TBase>.Default.Decrement(ToBase(value)));

    private static TBase ToBase(T value) => Unsafe.As<T, TBase>(ref value);
    private static T FromBase(TBase value) => Unsafe.As<TBase, T>(ref value);
  }

  public class BigIntegerArithmetics<T> : UnmanagedArithmetics<T> where T : unmanaged
  {
    public static BigIntegerArithmetics<T> Instance { get; } = new BigIntegerArithmetics<T>();

    public override T Increment(T value)
    {
      var newValue = ToBigInteger(value) + 1;
      return FromBigInteger(newValue);
    }

    public override T Decrement(T value)
    {
      var newValue = ToBigInteger(value) - 1;
      return FromBigInteger(newValue);
    }

    private static unsafe BigInteger ToBigInteger(T value)
    {
      var ptr = Unsafe.AsPointer(ref value);
      var span = new ReadOnlySpan<byte>(ptr, sizeof(T));
      return new BigInteger(span);
    }
    
    private static unsafe T FromBigInteger(BigInteger bigInteger)
    {
      var value = default(T);
      var ptr = Unsafe.AsPointer(ref value);
      var spanValue = new Span<byte>(ptr, sizeof(T));
      if (bigInteger.TryWriteBytes(spanValue, out var bytesWritten) && bytesWritten <= sizeof(T))
        return value;
      
      throw new InvalidOperationException($"Can't convert BigInteger value: {bigInteger} to {typeof(T).Name}");
    }
  }
}