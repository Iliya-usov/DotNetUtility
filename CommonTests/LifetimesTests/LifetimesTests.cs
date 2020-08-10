using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Lifetimes;
using Common.Logging;
using NUnit.Framework;
using TestUtils;

namespace CommonTests.LifetimesTests
{
  public class LifetimesTests
  {
    public static TimeSpan DefaultTimeout => Debugger.IsAttached ? TimeSpan.FromDays(1) : TimeSpan.FromSeconds(5); 
    private ErrorsDetector? myDetector;
    
    [SetUp]
    public void SetUp()
    {
      myDetector = new ErrorsDetector();
      LogManager.Instance.AddListeners(myDetector);
    }

    [TearDown]
    public void TearDown()
    {
      // todo refator
      GC.GetTotalMemory(true);
      var detector = myDetector!; 
      LogManager.Instance.RemoveListeners(detector);
      var errors = detector.Errors;
      myDetector = null;
      if (errors.IsNotEmpty())
        Assert.Fail(string.Join(Environment.NewLine, errors));
    }

    [Test]
    public void SimpleTerminationTest()
    {
      var terminatedCount = 0;
      var def = new LifetimeDefinition();
      def.Lifetime.IsAlive.ShouldBeTrue();

      def.Lifetime.OnTermination(() => terminatedCount++);
      def.Lifetime.TryOnTermination(() => terminatedCount++).ShouldBeTrue();
      
      def.Lifetime.IsAlive.ShouldBeTrue();
      terminatedCount.ShouldBe(0);

      def.Terminate();
      def.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      terminatedCount.ShouldBe(2);
    }

    [Test]
    public void ReverseSequenceTerminationTest()
    {
      var count = 0;
      var def = new LifetimeDefinition();
      for (var i = 0; i < 4; i++) 
        def.Lifetime.Bracket(() => count++, value => value.ShouldBe(--count));

      def.Terminate();
      def.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      count.ShouldBe(0);
    }

    [Test]
    public void DisposableTest()
    {
      var def = new LifetimeDefinition();
      var disposable = new TestDisposable();
      def.Lifetime.OnTermination(disposable);
      def.Lifetime.TryOnTermination(disposable).ShouldBeTrue();
      
      def.Terminate();
      disposable.Disposed.ShouldBeTrue();

      Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.OnTermination(disposable));
      def.Lifetime.TryOnTermination(disposable).ShouldBeFalse();
    }

    [Test]
    public void CircularTerminationTest()
    {
      var def = new LifetimeDefinition();
      for (var i = 0; i < 100; i++) 
        def.Lifetime.OnTermination(() => def.Terminate());
      def.Terminate();
    }

    [Test]
    public void CancellationTest()
    {
      var def1 = new LifetimeDefinition();
      var def2 = def1.Lifetime.DefineNested();
      var def3 = def2.Lifetime.DefineNested();
      var def4 = def3.Lifetime.DefineNested();
      var def5 = new LifetimeDefinition();

      def3.Lifetime.OnTermination(() =>
      {
        def1.Lifetime.Status.ShouldBe(LifetimeStatus.Terminating);
        def2.Lifetime.Status.ShouldBe(LifetimeStatus.Terminating);
        def3.Lifetime.Status.ShouldBe(LifetimeStatus.Terminating);
        
        def4.Lifetime.Status.ShouldBe(LifetimeStatus.Cancelling);
        
        def5.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      });

      def3.AttachOrThrow(def5);
      
      def1.Terminate();
      
      def1.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      def2.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      def3.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      def4.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
    }

    [Test]
    public void NestedTerminationTest()
    {
      var defTerminationCount = 0;
      var nestedTerminationCount = 0;
      
      var def = new LifetimeDefinition();
      def.Lifetime.OnTermination(() => defTerminationCount++);
      var nested = def.Lifetime.DefineNested();
      nested.Lifetime.OnTermination(() => nestedTerminationCount++);
      
      defTerminationCount.ShouldBe(0);
      nestedTerminationCount.ShouldBe(0);
      
      def.Lifetime.Status.ShouldBe(LifetimeStatus.Alive);
      nested.Lifetime.Status.ShouldBe(LifetimeStatus.Alive);
      
      nested.Terminate();
      
      defTerminationCount.ShouldBe(0);
      nestedTerminationCount.ShouldBe(1);
      
      def.Lifetime.Status.ShouldBe(LifetimeStatus.Alive);
      nested.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      
      nested.Terminate();
      
      defTerminationCount.ShouldBe(0);
      nestedTerminationCount.ShouldBe(1);
      
      def.Lifetime.Status.ShouldBe(LifetimeStatus.Alive);
      nested.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      
      def.Terminate();
      
      defTerminationCount.ShouldBe(1);
      nestedTerminationCount.ShouldBe(1);
      
      def.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      nested.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);

      var nested2 = def.Lifetime.DefineNested();
      nested2.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
    }

    [Test]
    public void ThrowLifetimeCanceledExceptionTest()
    {
      for (var i = 0; i < 100; i++)
      {
        var canTerminate = false;
        var def = new LifetimeDefinition();
        def.Lifetime.ThrowIfNotAlive();
        
        // ReSharper disable once AccessToModifiedClosure
        def.Lifetime.OnTermination(() => ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => canTerminate, DefaultTimeout));
        var task = Task.Run(() => def.Terminate());

        ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => def.Lifetime.Status != LifetimeStatus.Alive, DefaultTimeout);
        
        def.Lifetime.IsAlive.ShouldBeFalse();
        def.Lifetime.IsNotAlive.ShouldBeTrue();

        def.TryAttach(def).ShouldBeFalse();
        def.Lifetime.TryOnTermination(def).ShouldBeFalse();
        def.Lifetime.TryOnTermination(() => {}).ShouldBeFalse();
        def.Lifetime.TryKeepAlive(def).ShouldBeFalse();
        def.Lifetime.TryBracket(() => {}, () => {}).ShouldBeFalse();
        def.Lifetime.TryBracket(() => 1, _ => {}).Success.ShouldBeFalse();

        Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.ThrowIfNotAlive());
        Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.OnTermination(() => { }));
        Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.OnTermination(def));
        Assert.Throws<LifetimeCanceledException>(() => def.AttachOrThrow(def));
        Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.Bracket(() => { }, () => { }));
        Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.Bracket(() => 1, _ => { }));
        Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.KeepAlive(def));

        canTerminate = true;
        task.Wait(DefaultTimeout);
        def.Lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      }
    }

    [Test]
    public void ConcurrentTermination()
    {
      const int n = 20;
      
      for (var i = 0; i < n * n; i++)
      {
        var terminationCount = 0;
        var def = new LifetimeDefinition();
      
        // ReSharper disable once AccessToModifiedClosure
        def.Lifetime.OnTermination(() => Interlocked.Increment(ref terminationCount));

        ConcurrentTestUtil.ParallelInvoke(n, () => def.Terminate(), DefaultTimeout);
        terminationCount.ShouldBe(1); 
      }

      for (var i = 0; i < n * n; i++)
      {
        var count = 0;
        var waited = false;
        var def = new LifetimeDefinition();
        def.Lifetime.OnTermination(() =>
        {
          ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => waited = count == n, DefaultTimeout);
        });
        
        ConcurrentTestUtil.ParallelInvoke(n + 1, () =>
        {
          def.Terminate();
          if (waited) return;
          Interlocked.Increment(ref count).ShouldBeLessThan(n + 1);
        }, DefaultTimeout);
      }
    }

    [Test]
    public void SequentialLifetimesTest()
    {
      var def = new LifetimeDefinition();
      var lifetimes = def.Lifetime.DefineNestedSequential();

      var count = 0;
      for (var i = 0; i < 4; i++)
      {
        lifetimes.Next().OnTermination(() => count++);
        count.ShouldBe(i);
      }

      var lifetime1 = lifetimes.Next();
      lifetime1.Status.ShouldBe(LifetimeStatus.Alive);
      
      var lifetime2 = lifetimes.Next();
      lifetime1.Status.ShouldBe(LifetimeStatus.Terminated);
      lifetime2.Status.ShouldBe(LifetimeStatus.Alive);
      
      lifetimes.TerminateCurrent();
      lifetime2.Status.ShouldBe(LifetimeStatus.Terminated);

      var lifetime = lifetimes.Next();
      lifetime.Status.ShouldBe(LifetimeStatus.Alive);
      
      def.Terminate();
      lifetime.Status.ShouldBe(LifetimeStatus.Terminated);

      var terminatedNext = lifetimes.Next();
      terminatedNext.Status.ShouldBe(LifetimeStatus.Terminated);
      
      def.Status.ShouldBe(LifetimeStatus.Terminated);
    }
    
    [Test]
    public void SequentialLifetimesConcurrentNextTest()
    {
      const int n = 20;
      for (var i = 0; i < n * n; i++)
      {
        var lifetimes = new SequentialLifetimes();
        var lifetimesArray = new Lifetime[n];
        var count = -1;
        ConcurrentTestUtil.ParallelInvoke(n, () =>
        {
          var lifetime = lifetimes.Next();
          lifetimesArray[Interlocked.Increment(ref count)] = lifetime;
        }, DefaultTimeout);
        
        lifetimesArray.Count(x => x.IsAlive).ShouldBe(1);
        lifetimesArray.Distinct(Lifetime.Comparer.Instance).Count().ShouldBe(n);
        lifetimes.TerminateCurrent();
        
        lifetimesArray.Count(x => x.IsAlive).ShouldBe(0);
      }
    }

    [Test]
    public void ExecuteIfAliveTest()
    {
      var oldDefault = LifetimeDefinition.TerminationUnderExecutionTimeout;
      LifetimeDefinition.TerminationUnderExecutionTimeout = TimeSpan.FromSeconds(5);
      try
      {
        var terminatingEnded = false;
        var terminationCalled = false;
        var def = new LifetimeDefinition();
        // ReSharper disable once AccessToModifiedClosure
        def.Lifetime.OnTermination(() => terminationCalled = true);
        
        using (var cookie = def.UsingExecuteIfAlive())
        {
          cookie.Success.ShouldBeTrue();

          var terminatingStarted = false;

          Task.Run(() =>
          {
            terminatingStarted = true;
            def.Terminate();
            // ReSharper disable once AccessToModifiedClosure
            terminatingEnded = true;
          });

          ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => terminatingStarted, TimeSpan.FromSeconds(5));

          for (int i = 0; i < 5; i++)
          {
            Thread.Sleep(100);
            Assert.True(def.Lifetime.Status < LifetimeStatus.Terminating, "def.Status < LifetimeStatus.Terminating");
            terminatingEnded.ShouldBeFalse();
          }

          def.Status.ShouldBe(LifetimeStatus.Cancelling);

          terminationCalled.ShouldBeFalse();
        }
        
        ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => terminatingEnded, DefaultTimeout);
        terminationCalled.ShouldBeTrue();
        def.Status.ShouldBe(LifetimeStatus.Terminated);
      }
      finally
      {
        LifetimeDefinition.TerminationUnderExecutionTimeout = oldDefault;
      }
    }

    [Test]
    public void TerminationUnderExecutionTest()
    {
      var def = new LifetimeDefinition();
      def.Lifetime.Execute(() => Assert.Throws<InvalidOperationException>(() => def.Terminate()));
      def.Status.ShouldBe(LifetimeStatus.Alive);
      
      def.Lifetime.Execute(() => { Assert.Throws<InvalidOperationException>(() => def.Terminate()); });
      def.Status.ShouldBe(LifetimeStatus.Alive);
      
      def.Lifetime.TryExecute(() => Assert.Throws<InvalidOperationException>(() => def.Terminate())).Success.ShouldBeTrue();
      def.Status.ShouldBe(LifetimeStatus.Alive);
      
      def.Lifetime.TryExecute(() => { Assert.Throws<InvalidOperationException>(() => def.Terminate()); }).ShouldBeTrue();
      def.Status.ShouldBe(LifetimeStatus.Alive);

      def.Terminate();

      var count = 0;
      
      Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.Execute(() => { ++count;}));
      count.ShouldBe(0);

      Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.Execute(() => ++count));
      count.ShouldBe(0);
      
      def.Lifetime.TryExecute(() => { ++count; }).ShouldBeFalse();
      count.ShouldBe(0);

      def.Lifetime.TryExecute(() => ++count).Success.ShouldBeFalse();
      count.ShouldBe(0);
    }

    [Test]
    public void CancellationTokenTest()
    {
      var def = new LifetimeDefinition();
      var token = def.Lifetime.ToCancellationToken();
      token.IsCancellationRequested.ShouldBeFalse();
      def.Terminate();
      token.IsCancellationRequested.ShouldBeTrue();
      token.ShouldBe(def.Lifetime);

      const int n = 20;
      for (var i = 0; i < n * n; i++)
      {
        var count = -1;
        var definition = new LifetimeDefinition();
        var tokens = new CancellationToken[n];
        ConcurrentTestUtil.ParallelInvoke(n, () =>
        {
          CancellationToken cancellationToken = definition.Lifetime;
          tokens[Interlocked.Increment(ref count)] = cancellationToken;
        }, DefaultTimeout);
        
        tokens.All(x => !x.IsCancellationRequested).ShouldBeTrue();
        tokens.All(x => x == definition.ToCancellationToken()).ShouldBeTrue();
        
        definition.Terminate();
        tokens.All(x => x.IsCancellationRequested).ShouldBeTrue();
      }
    }

    [Test]
    public void LifetimeUsingTest()
    {
      var count = 0;
      Lifetime.Using(lifetime => lifetime.OnTermination(() => count++));
      count.ShouldBe(1);
      
      Lifetime.Using(lifetime =>
      {
        lifetime.OnTermination(() => count++);
        return count;
      }).ShouldBe(count - 1);
      count.ShouldBe(2);
      
      Lifetime.UsingAsync(async lifetime =>
      {
        await Task.Yield();
        lifetime.OnTermination(() => count++);
      }).Wait(DefaultTimeout);
      count.ShouldBe(3);
      
      Lifetime.UsingAsync(async lifetime =>
      {
        await Task.Yield();
        lifetime.OnTermination(() => count++);
        return count;
      }).Result.ShouldBe(count - 1);
      count.ShouldBe(4);
    }

    [Test]
    public void IntersectionsTest()
    {
      {
        var def1 = new LifetimeDefinition();
        var def2 = new LifetimeDefinition();

        var intersect = def1.Lifetime.Intersect(def2.Lifetime);
        intersect.Status.ShouldBe(LifetimeStatus.Alive);
        
        def1.Terminate();
        intersect.Status.ShouldBe(LifetimeStatus.Terminated);
        
        def2.Terminate();
        intersect.Status.ShouldBe(LifetimeStatus.Terminated);        
      }
      
      {
        var def1 = new LifetimeDefinition();
        var def2 = new LifetimeDefinition();

        var intersect = def1.Lifetime.Intersect(def2.Lifetime);
        intersect.Status.ShouldBe(LifetimeStatus.Alive);
        
        def2.Terminate();
        intersect.Status.ShouldBe(LifetimeStatus.Terminated);
        
        def1.Terminate();
        intersect.Status.ShouldBe(LifetimeStatus.Terminated);
      }

      const int n = 20;
      for (var i = 0; i < n * n; i++)
      {
        var definitions = Enumerable.Range(0, n).Select(x=> new LifetimeDefinition()).ToArray();
        var lifetime = Lifetime.Intersect(definitions.Select(x => x.Lifetime).ToArray());
     
        ConcurrentTestUtil.ParallelInvoke(n, j => definitions[j].Terminate(), DefaultTimeout);
        lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      }
      
      for (var i = 0; i < n * n; i++)
      {
        var definitions = Enumerable.Range(0, n).Select(x=> new LifetimeDefinition()).ToArray();
        var lifetimes = definitions.Select(x => x.Lifetime).ToArray();
        
        var count = 0;
        var task = ConcurrentTestUtil.ParallelInvokeAsync(n, j =>
        {
          // ReSharper disable once AccessToModifiedClosure
          Interlocked.Increment(ref count);
          ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => count == n + 1, DefaultTimeout);
          definitions[j].Terminate();
        });

        Interlocked.Increment(ref count);
        ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => count == n + 1, DefaultTimeout);
        var lifetime = Lifetime.Intersect(lifetimes);
        task.Wait(DefaultTimeout);
        lifetime.Status.ShouldBe(LifetimeStatus.Terminated);
      }
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void LifetimeStartTest()
    {
      var comparer = Lifetime.Comparer.Instance;
      using var def = new LifetimeDefinition();
      {
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        var task = def.Lifetime.Start(() => comparer.Equals(Lifetime.AsyncLocal.Value, def.Lifetime).ShouldBeTrue());
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        task.Wait(DefaultTimeout);
      }
      {
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        var task = def.Lifetime.Start(() => Lifetime.AsyncLocal.Value);
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        comparer.Equals(task.Result, def.Lifetime).ShouldBeTrue();
      }
      {
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        var task = def.Lifetime.StartAsync(async () =>
        {
          comparer.Equals(Lifetime.AsyncLocal.Value, def.Lifetime).ShouldBeTrue();
          await Task.Yield();
          comparer.Equals(Lifetime.AsyncLocal.Value, def.Lifetime).ShouldBeTrue();
        });
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        task.Wait(DefaultTimeout);
      }
      {
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        var task = def.Lifetime.StartAsync(async () =>
        {
          comparer.Equals(Lifetime.AsyncLocal.Value, def.Lifetime).ShouldBeTrue();
          await Task.Yield();
          comparer.Equals(Lifetime.AsyncLocal.Value, def.Lifetime).ShouldBeTrue();
          return Lifetime.AsyncLocal.Value;
        });
        comparer.Equals(Lifetime.AsyncLocal.Value, Lifetime.Eternal).ShouldBeTrue();
        comparer.Equals(task.Result, def.Lifetime).ShouldBeTrue();
      }
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public void ConcurrentAddTerminationHandlersTest()
    {
      const int n = 20;
      for (var i = 0; i < n * n; i++)
      {
        var array = new bool[n];
        var def = new LifetimeDefinition();

        ConcurrentTestUtil.ParallelInvoke(n, j => def.Lifetime.OnTermination(() => array[j] = true), DefaultTimeout);
        array.All(x => !x).ShouldBeTrue();
        def.Terminate();
        array.All(x => x).ShouldBeTrue();
      }
      
      for (var i = 0; i < n * n; i++)
      {
        var def = new LifetimeDefinition();
        var count = 0;
        var task = ConcurrentTestUtil.ParallelInvokeAsync(n, j =>
        {
          if (j == n / 2) def.Terminate();
          else
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() =>
            {
              def.Lifetime.OnTermination(() => Interlocked.Decrement(ref count));
              Interlocked.Increment(ref count);
              return false;
            }, DefaultTimeout);
          }
        });
        var aggregateException = Assert.Throws<AggregateException>(() => task.Wait(DefaultTimeout));
        aggregateException.Flatten().InnerExceptions.All(x => x is LifetimeCanceledException).ShouldBeTrue();

        count.ShouldBe(0);
      }
      
      for (var i = 0; i < n * n; i++)
      {
        var def = new LifetimeDefinition();
        var count = 0;
        var task = ConcurrentTestUtil.ParallelInvokeAsync(n, j =>
        {
          if (j == n / 2) def.Terminate();
          else
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() =>
            {
              if (def.TryOnTermination(() => Interlocked.Decrement(ref count)))
              {
                Interlocked.Increment(ref count);
                return false;
              }

              return true;
            }, DefaultTimeout);
          }
        });
        task.Wait(DefaultTimeout);
        task.Status.ShouldBe(TaskStatus.RanToCompletion);
        
        count.ShouldBe(0);
      }
    }

    [Test]
    public void SimpleBracketTest()
    {
      var random = new Random();
      var def = new LifetimeDefinition();
      var count = 0;
      def.Lifetime.Bracket(() => count++, () => count--);
      def.Lifetime.Bracket(() =>
      {
        var next = random.Next(1);
        count += next;
        return next;
      }, value => count -= value);
      
      def.Lifetime.TryBracket(() => count++, () => count--).ShouldBeTrue();
      def.Lifetime.TryBracket(() =>
      {
        var next = random.Next(1);
        count += next;
        return next;
      }, value => count -= value).Success.ShouldBeTrue();

      Assert.True(count > 0, "savedCount > 0");
      
      def.Terminate();
      count.ShouldBe(0);

      Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.Bracket(() => count++, () => {}));
      count.ShouldBe(0);
      
      Assert.Throws<LifetimeCanceledException>(() => def.Lifetime.Bracket( () => count += random.Next(1), value => {}));
      count.ShouldBe(0);
      
      def.Lifetime.TryBracket(() => count++, () => {}).ShouldBeFalse();
      count.ShouldBe(0);
      
      def.Lifetime.TryBracket(() => count += random.Next(1), value => {}).Success.ShouldBeFalse();
      count.ShouldBe(0);
    }

    [Test]
    public void ConcurrentBracketTest()
    {
      const int n = 20;
      
      for (var i = 0; i < n * n; i++)
      {
        var def = new LifetimeDefinition();
        var count = 0;
        var task = ConcurrentTestUtil.ParallelInvokeAsync(n, j =>
        {
          if (j == n / 2)
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => count > 10, DefaultTimeout);
            def.Terminate();
          }
          else
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() =>
            {
              var isNotAlive = def.IsNotAlive;
              def.Lifetime.Bracket(() => Interlocked.Increment(ref count), () => Interlocked.Decrement(ref count));
              return isNotAlive;
            }, DefaultTimeout);
          }
        });
        
        var aggregateException = Assert.Throws<AggregateException>(() => task.Wait(DefaultTimeout));
        aggregateException.Flatten().InnerExceptions.All(x => x is LifetimeCanceledException).ShouldBeTrue();
      
        task.IsCompleted.ShouldBeTrue();
        def.Status.ShouldBe(LifetimeStatus.Terminated);
        count.ShouldBe(0);
      }
      
      for (var i = 0; i < n * n; i++)
      {
        var def = new LifetimeDefinition();
        var count = 0;
        var task = ConcurrentTestUtil.ParallelInvokeAsync(n, j =>
        {
          if (j == n / 2)
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => count > 10, DefaultTimeout);
            def.Terminate();
          }
          else
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() =>
            {
              return !def.Lifetime.TryBracket(() => Interlocked.Increment(ref count), () => Interlocked.Decrement(ref count));
            }, DefaultTimeout);
          }
        });
        
        task.Wait(DefaultTimeout);
        task.Status.ShouldBe(TaskStatus.RanToCompletion);
        
        count.ShouldBe(0);
      }
      
      for (var i = 0; i < n * n; i++)
      {
        var def = new LifetimeDefinition();
        var count = 0;
        var task = ConcurrentTestUtil.ParallelInvokeAsync(n, j =>
        {
          if (j == n / 2)
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => count > 100, DefaultTimeout);
            def.Terminate();
          }
          else
          {
            var random = new Random();
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() =>
            {
              var isNotAlive = def.IsNotAlive;
              def.Lifetime.Bracket(() =>
              {
                var next = random.Next(1, 10);
                Interlocked.Add(ref count, next);
                return next;
              }, next => Interlocked.Add(ref count, -next));
              return isNotAlive;
            }, DefaultTimeout);
          }
        });
        
        var aggregateException = Assert.Throws<AggregateException>(() => task.Wait(DefaultTimeout));
        aggregateException.Flatten().InnerExceptions.All(x => x is LifetimeCanceledException).ShouldBeTrue();
      
        count.ShouldBe(0);
      }
      
      for (var i = 0; i < n * n; i++)
      {
        var def = new LifetimeDefinition();
        var count = 0;
        var task = ConcurrentTestUtil.ParallelInvokeAsync(n, j =>
        {
          if (j == n / 2)
          {
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => count > 100, DefaultTimeout);
            def.Terminate();
          }
          else
          {
            var random = new Random();
            ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() =>
            {
              return !def.Lifetime.TryBracket(() =>
              {
                var next = random.Next(1, 10);
                Interlocked.Add(ref count, next);
                return next;
              }, next => Interlocked.Add(ref count, -next)).Success;
            }, DefaultTimeout);
          }
        });

        task.Wait(DefaultTimeout);
        task.Status.ShouldBe(TaskStatus.RanToCompletion);

        count.ShouldBe(0);
      }
    }

    [Test]
    public void CanceledTaskTest()
    {
      for (int i = 0; i < 100; i++)
      {
        var def = new LifetimeDefinition();
        var started = false;
        var task = Task.Factory.StartNew(() =>
        {
          started = true;
          ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => def.Lifetime.IsNotAlive, DefaultTimeout);
          def.Lifetime.ThrowIfNotAlive();
        }, def.Lifetime);

        ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => started, DefaultTimeout);
        def.Terminate();

        ConcurrentTestUtil.SpinUntilOrThrowIfTimeout(() => task.IsCompleted, DefaultTimeout);
        task.IsCanceled.ShouldBeTrue(); 
      }
    }

    [Test]
    public void LifetimeCancellationTokenTest()
    {
      {
        var def = new LifetimeDefinition();
        
        def.Terminate();
        def.Lifetime.ToCancellationToken().IsCancellationRequested.ShouldBeTrue();
        def.Lifetime.ToCancellationToken().ShouldBe(new CancellationToken(true));
      }
      
      {
        var def = new LifetimeDefinition();
        var token = def.ToCancellationToken();
        token.IsCancellationRequested.ShouldBeFalse();
        def.Lifetime.ToCancellationToken().ShouldBe(token);
        
        def.Terminate();
        token.IsCancellationRequested.ShouldBeTrue();
        def.Lifetime.ToCancellationToken().IsCancellationRequested.ShouldBeTrue();
        def.Lifetime.ToCancellationToken().ShouldNotBe(new CancellationToken(true));
        def.Lifetime.ToCancellationToken().ShouldBe(token);
      }
      
      {
        var def = new LifetimeDefinition();
        for (var i = 0; i < 10000; i++) def.Lifetime.OnTermination(() => { });
        def.Terminate();
          
        def.Lifetime.ToCancellationToken().IsCancellationRequested.ShouldBeTrue();
        def.Lifetime.ToCancellationToken().ShouldNotBe(new CancellationToken(true));
        def.Lifetime.ToCancellationToken().ShouldBe(def.ToCancellationToken());
      }
    }
  }
    
  internal class TestDisposable : IDisposable
  {
    public bool Disposed { get; private set; }
    public void Dispose() => Disposed = true;
  }

  public class ErrorsDetector : ILogListener
  {
    private readonly ConcurrentBag<string> myErrors = new ConcurrentBag<string>();

    public List<string> Errors => myErrors.ToList();
    
    public void OnLog(in LogEvent logEvent)
    {
      if (logEvent.Level >= LoggingLevel.Error)
        myErrors.Add(logEvent.Present().ToString());
    }
  }
}