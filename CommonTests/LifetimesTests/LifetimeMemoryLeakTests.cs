using System;
using Common.Lifetimes;
using NUnit.Framework;
using TestUtils;

namespace CommonTests.LifetimesTests
{
  public class LifetimeMemoryLeakTest
  {
    private WeakReference<TestDisposable?> myWeakReference = null!;
    private LifetimeDefinition myLifetimeDefinition = null!;
    
    [SetUp]
    public void SetUp()
    {
      myWeakReference = new WeakReference<TestDisposable?>(null);
      myLifetimeDefinition = new LifetimeDefinition();
    }

    [TearDown]
    public void TearDown()
    {
      myLifetimeDefinition.ShouldNotBeNull();
      myLifetimeDefinition.Terminate();
      _ = GC.GetTotalMemory(true);
      
      myWeakReference.TryGetTarget(out _).ShouldBeFalse();
    }

    [Test]
    public void SimpleDisposableMemLeakTest()
    {
      var testDisposable = new TestDisposable();
      myWeakReference.SetTarget(testDisposable);

      myLifetimeDefinition.Lifetime.OnTermination(testDisposable);
      myLifetimeDefinition.Lifetime.OnTermination(() => testDisposable.Dispose());
      myLifetimeDefinition.Lifetime.OnTermination(testDisposable.Dispose);
    }
    
    [Test]
    public void ManyHandlersDisposableMemLeakTest()
    {
      var testDisposable = new TestDisposable();
      myWeakReference.SetTarget(testDisposable);

      for (var i = 0; i < 1000; i++)
      {
        myLifetimeDefinition.Lifetime.OnTermination(testDisposable);
        myLifetimeDefinition.Lifetime.OnTermination(() => testDisposable.Dispose());
        myLifetimeDefinition.Lifetime.OnTermination(testDisposable.Dispose);
      }
    }
  }
}