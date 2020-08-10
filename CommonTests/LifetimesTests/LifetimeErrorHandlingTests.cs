using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Common.Lifetimes;
using Common.Logging;
using NUnit.Framework;
using TestUtils;

namespace CommonTests.LifetimesTests
{
  public class LifetimeErrorHandlingTests
  {
    [Test]
    public void ExceptionOnTerminationHandlingTest()
    {
      using var collector = new ErrorsCollector();

      Lifetime.Using(lf => lf.OnTermination(() => throw new TestException()));
      var evensList = collector.GetEvensList();
      evensList.Count.ShouldBe(1);
      var logEvent = evensList.Single();
      var exception = logEvent.Exception.ShouldNotBeNull()!;
      exception.ShouldBeIs<TestException>();
      logEvent.Level.ShouldBe(LoggingLevel.Error);
    }
    
    private class TestException : Exception { }
    
    private class ErrorsCollector : ILogListener, IDisposable
    {
      private readonly ConcurrentBag<LogEvent> myBag = new ConcurrentBag<LogEvent>();
      public List<LogEvent> GetEvensList() => myBag.ToList();
      
      public void OnLog(in LogEvent logEvent)
      {
        myBag.Add(logEvent);
      }

      public ErrorsCollector() => LogManager.Instance.AddListeners(this);
      public void Dispose() => LogManager.Instance.RemoveListeners(this);
    }
  }
}