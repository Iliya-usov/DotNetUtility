using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Common.Caches.Timestamped
{
  public static class TimestampedCache
  {
    public const int InvalidTimestamp = int.MinValue;
    public const int InitTimestamp = int.MinValue + 1;

    public static void IncrementTimestamp(ref int timestamp)
    {
      if (Interlocked.Increment(ref timestamp) == InvalidTimestamp) 
        Interlocked.Increment(ref timestamp);
    }
  }

  public class TimestampedCache<T>
  {
    private readonly Func<T> myFactory;
    private readonly Func<int> myCheckTimestamp;
    private readonly object myLock = new object();

    private int myTimestamp;

    [AllowNull] private T myValue;

    public TimestampedCache(Func<T> factory, Func<int> checkTimestamp)
    {
      myValue = default;
      myFactory = factory;
      myCheckTimestamp = checkTimestamp;
      myTimestamp = TimestampedCache.InvalidTimestamp;
    }

    public T GetValue()
    {
      var (value, timestamp) = GetInfo();
      var expectedTimestamp = myCheckTimestamp();
      if (expectedTimestamp == timestamp && timestamp != TimestampedCache.InvalidTimestamp)
        return value;
      
      value = myFactory();
      lock (myLock)
      {
        myValue = value;
        myTimestamp = expectedTimestamp;
      }
      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (T value, int timestamp) GetInfo()
    {
      lock (myLock) return (myValue, myTimestamp);
    }
  }
}