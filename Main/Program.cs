using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Common.NativeInterop;
using Common.Tasks;

namespace Main
{
  public struct E
  {
    public int Value;
  }

  public static class Program
  {
    public static unsafe void Main(string[] args)
    {
      var scheduler = CpuBoundTaskScheduler.Default;
      int count = 0;

      var stopwatch = Stopwatch.StartNew();
      var results = Enumerable.Range(0, 10000).Select(x =>
      {
        return Task.Factory.StartNew(() =>
        {
          var sw = Stopwatch.StartNew();
          Thread.Sleep(10);
          Console.WriteLine(sw.ElapsedMilliseconds);
          Interlocked.Increment(ref count);
        }, CancellationToken.None, TaskCreationOptions.None, scheduler);
      }).ToArray();
      
      Task.WaitAll(results);
      Console.WriteLine();
      Console.WriteLine(stopwatch.ElapsedMilliseconds);
      Console.WriteLine();
      Console.WriteLine(count);
    }

    public class A
    {
      public int Property => 1;
      public string Property2 => "ndkf";
      public List<int> List => new List<int>() {1, 2, 3, 4};

      public int GetValue() => Property + 1;

      public void Do()
      {
        int count = 0;
        while (true)
        {
          var lazy = new Lazy<int>(() =>
          {
            Debugger.NotifyOfCrossThreadDependency();
            return 2;
          });
          Console.WriteLine(count);
        }
      }
    }

    public interface IInterface
    {
      [NativeImport("huita")]
      int GetI();

      int GetLength(string s);
    }

    public static int M()
    {
      return 1;
    }


    public class MyClass
    {
      public Func<int, string, object, double, int, int, string> myD;

      public string D(int i1, string i2, object i3, double i4, int i5, int i6)
      {
        return myD(i1, i2, i3, i4, i5, i6);
      }
    }


    public static IEnumerable<int> Enum()
    {
      yield return 1;
      yield return 2;
      yield return 3;
      yield return 4;
      yield return 5;
      yield return 6;
      yield return 7;
      yield return 8;
    }
  }

  public class Test
  {
    private VolatileTest myVolatileTest;
    private VolatileTest2 myVolatileTest2;
    private SimpleTest mySimpleTest;

    public Test()
    {
      myVolatileTest = new VolatileTest();
      myVolatileTest2 = new VolatileTest2();

      mySimpleTest = new SimpleTest();
    }

    [Benchmark]
    public int VolatileTest()
    {
      var s = 0;
      for (int i = 0; i < 10; i++)
      {
        myVolatileTest.A = i + myVolatileTest.B;
        myVolatileTest.B = i;
        s += myVolatileTest.Sum(i);
      }

      return s;
    }

    [Benchmark]
    public int SimpleTest()
    {
      var s = 0;
      for (int i = 0; i < 10; i++)
      {
        mySimpleTest.A = i + mySimpleTest.B;
        mySimpleTest.B = i;
        s += mySimpleTest.Sum(i);
      }

      return s;
    }

    [Benchmark]
    public int VolatileTest2()
    {
      var s = 0;
      for (int i = 0; i < 10; i++)
      {
        Volatile.Write(ref myVolatileTest2.A, i + Volatile.Read(ref myVolatileTest2.B));
        Volatile.Write(ref myVolatileTest2.B, i);
        s += myVolatileTest2.Sum(i);
      }

      return s;
    }
  }

  public class VolatileTest
  {
    public volatile int A;
    public volatile int B;

    public int Sum(int i)
    {
      return (A + i) * B;
    }
  }

  public class VolatileTest2
  {
    public int A;
    public int B;

    public int Sum(int i)
    {
      return (Volatile.Read(ref A) + i) * Volatile.Read(ref B);
    }
  }

  public class SimpleTest
  {
    public int A;
    public int B;

    public int Sum(int i)
    {
      return (A + i) * B;
    }
  }
}