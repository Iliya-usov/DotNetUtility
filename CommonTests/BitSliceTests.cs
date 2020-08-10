using System;
using System.Diagnostics;
using System.Threading;
using Common.DataStructures;
using NUnit.Framework;
using TestUtils;

namespace CommonTests
{
  public class BitSliceTests
  {  
    public static TimeSpan DefaultTimeout => Debugger.IsAttached ? TimeSpan.FromDays(1) : TimeSpan.FromSeconds(5); 

    [Test]
    public void SimpleIntBitSliceTest()
    {
      var set = 0;

      var byte1 = new IntBitSlice<byte>();
      var short1 = byte1.Next<short>();
      var byte2 = short1.Next<byte>();

      Assert.Throws<ArgumentException>(() => _ = new IntBitSlice<byte>(4));
      Assert.Throws<ArgumentException>(() => _ = byte2.Next<byte>());

      byte1.ReadValue(ref set).ShouldBe(default);
      byte2.ReadValue(ref set).ShouldBe(default);
      short1.ReadValue(ref set).ShouldBe(default);

      byte1.Update(ref set, 37);
      byte2.Update(ref set, 250);
      short1.Update(ref set, 381);
      
      byte1.ReadValue(ref set).ShouldBe<byte>(37);
      byte2.ReadValue(ref set).ShouldBe<byte>(250);
      short1.ReadValue(ref set).ShouldBe<short>(381);

      byte1.Update(ref set,2);
      byte2.Update(ref set,3);
      short1.Update(ref set,4);
      
      byte1.ReadValue(ref set).ShouldBe<byte>(2);
      byte2.ReadValue(ref set).ShouldBe<byte>(3);
      short1.ReadValue(ref set).ShouldBe<short>(4);

      byte1.Increment(ref set).ShouldBe<byte>(3);
      byte2.Increment(ref set).ShouldBe<byte>(4);
      short1.Increment(ref set).ShouldBe<short>(5);
      
      byte1.ReadValue(ref set).ShouldBe<byte>(3);
      byte2.ReadValue(ref set).ShouldBe<byte>(4);
      short1.ReadValue(ref set).ShouldBe<short>(5);
    }

    [Test]
    public void ConcurrentIncrementTest()
    {
      const int n = 20;
      var set = 0;
      var slice1 = new IntBitSlice<ushort>();
      var slice2 = slice1.Next<ushort>();
      
      ConcurrentTestUtil.ParallelInvoke(n, i =>
      {
        for (var j = 0; j < n * n * 2; j++)
        {
          // ReSharper disable once AccessToModifiedClosure
          if (j % 2 == 0) slice1.Increment(ref set);
          else slice2.Increment(ref set);

          Thread.Yield();
        }
      }, DefaultTimeout);

      var count1 = slice1.ReadValue(ref set);
      var count2 = slice2.ReadValue(ref set);

      count1.ShouldBe(count2);
      count1.ShouldBe<ushort>(n * n * n);
      
      ConcurrentTestUtil.ParallelInvoke(n, i =>
      {
        for (var j = 0; j < n * n * 2; j++)
        {
          if (j % 2 == 0) slice1.Decrement(ref set);
          else slice2.Decrement(ref set);

          Thread.Yield();
        }
      }, DefaultTimeout);


      set.ShouldBe(0);
      
            
      ConcurrentTestUtil.ParallelInvoke(n, i =>
      {
        for (var j = 0; j < n * n * 2; j++)
        {
          // ReSharper disable once AccessToModifiedClosure
          if (j % 2 == 0) slice1.Increment(ref set);
          else slice2.Increment(ref set);

          Thread.Yield();
        }
      }, DefaultTimeout);
      
      ConcurrentTestUtil.ParallelInvoke(n, i =>
      {
        for (var j = 0; j < n * n * 2; j++)
        {
          var state = i % 4;
          if (state == 0) slice1.Increment(ref set);
          else if (state == 1) slice2.Increment(ref set);
          else if (state == 2) slice1.Decrement(ref set);
          else if (state == 3) slice2.Decrement(ref set);
          Thread.Yield();
        }
      }, DefaultTimeout);
      

      count1 = slice1.ReadValue(ref set);
      count2 = slice2.ReadValue(ref set);
      
      count1.ShouldBe(count2);
      count1.ShouldBe<ushort>(n * n * n);
    }
  }
}