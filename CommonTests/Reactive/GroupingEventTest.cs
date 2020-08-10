using System;
using System.Threading;
using Common.Lifetimes;
using Common.Reactive;
using NUnit.Framework;
using TestUtils;

namespace CommonTests.Reactive
{
  public class GroupingEventTest
  {
    public TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5); 
    
    [Test]
    public void SimpleGroupingEventTest()
    {
      var count = 0;
      
      using var def = new LifetimeDefinition();
      var groupingEvent = new GroupingEvent(
        def.Lifetime,
        "GroupingEvent",
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(50),
        () => count++);

      count.ShouldBe(0);
      
      groupingEvent.Fire();
      SpinWait.SpinUntil(() => count >= 1, DefaultTimeout).ShouldBeTrue();
      count.ShouldBe(1);
      
      Thread.Sleep(TimeSpan.FromMilliseconds(100));
      count.ShouldBe(1);
    }
    
    [Test]
    public void SimpleMaxDelayGroupingEventTest()
    {
      var count = 0;
      
      using var def = new LifetimeDefinition();
      var groupingEvent = new GroupingEvent(
        def.Lifetime,
        "GroupingEvent",
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(500),
        () => count++);

      count.ShouldBe(0);
      
      SpinWait.SpinUntil(() =>
      {
        groupingEvent.Fire();
        return count >= 1;
      }, DefaultTimeout).ShouldBeTrue();
      
      count.ShouldBe(1);
      
      Thread.Sleep(TimeSpan.FromMilliseconds(100));
      count.ShouldBe(2);
    }
    
    [Test]
    public void MassiveFireGroupingEventTest()
    {
      var count = 0;
      
      using var def = new LifetimeDefinition();
      var groupingEvent = new GroupingEvent(
        def.Lifetime,
        "GroupingEvent",
        TimeSpan.FromMilliseconds(100),
        DefaultTimeout * 10,
        () => count++);

      count.ShouldBe(0);

      SpinWait.SpinUntil(() =>
      {
        groupingEvent.Fire();
        return false;
      }, TimeSpan.FromMilliseconds(200));

      SpinWait.SpinUntil(() => count >= 1, DefaultTimeout).ShouldBe(true);
      
      count.ShouldBe(1);
    }
    
    [Test]
    public void MassiveMultiThreadFireGroupingEventTest()
    {
      const int n = 20;
      var count = 0;
      
      using var def = new LifetimeDefinition();
      var groupingEvent = new GroupingEvent(
        def.Lifetime,
        "GroupingEvent",
        TimeSpan.FromMilliseconds(100),
        DefaultTimeout * 10,
        () => count++);

      count.ShouldBe(0);

      ConcurrentTestUtil.ParallelInvoke(n, () =>
      {
        for (var i = 0; i < n * n; i++) 
          groupingEvent.Fire();
      }, TimeSpan.FromMilliseconds(100));

      SpinWait.SpinUntil(() => count >= 1, DefaultTimeout).ShouldBe(true);

      count.ShouldBe(1);
    }
  }
}