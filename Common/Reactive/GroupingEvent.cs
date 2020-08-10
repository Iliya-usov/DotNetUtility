using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Channels;
using Common.DataStructures;
using Common.Extensions;
using Common.Lifetimes;
using Common.Logging;

namespace Common.Reactive
{
  public class GroupingEvent
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<GroupingEvent>();

    private readonly Lifetime myLifetime;
    private readonly string myId;
    private readonly TimeSpan myDelay;
    private readonly TimeSpan myMaxDelay;
    private readonly Action myAction;

    private long myNextUtcTime;

    private readonly SequentialLifetimes myLifetimes;
    private readonly SequentialItemsProcessor myItemsProcessor;

    public GroupingEvent(Lifetime lifetime, string id, TimeSpan delay, TimeSpan maxDelay, Action action, TaskScheduler? scheduler = null)
    {
      myLifetimes = lifetime.DefineNestedSequential();
      myLifetime = lifetime;
      myId = id;
      myDelay = delay;
      myMaxDelay = maxDelay;
      myAction = action;
      myItemsProcessor = new SequentialItemsProcessor(lifetime, $"{id} Processor", scheduler);
    }

    public void Fire()
    {
      var now = DateTime.UtcNow;
      var nextUtcTime = now + myDelay;
      var nextUtcTimeTicks = nextUtcTime.Ticks;

      while (true)
      {
        if (Interlocked.CompareExchange(ref myNextUtcTime, nextUtcTimeTicks, 0) == 0)
          break;

        var oldNextUtcTimeTicks = Interlocked.Read(ref myNextUtcTime);
        if (oldNextUtcTimeTicks >= nextUtcTimeTicks) return;
        
        if (Interlocked.CompareExchange(ref myNextUtcTime, nextUtcTimeTicks, oldNextUtcTimeTicks) == oldNextUtcTimeTicks)
          return;
      }

      StartAsync(nextUtcTimeTicks, now);
    }

    public void FireImmediately()
    {
      myItemsProcessor.ProcessAsync(() =>
      {
        var oldNextUtcTimeTicks = Interlocked.Read(ref myNextUtcTime);
        try
        {
          myAction();
        }
        finally
        {
          if (Interlocked.CompareExchange(ref myNextUtcTime, 0, oldNextUtcTimeTicks) != oldNextUtcTimeTicks)
          {
            var now = DateTime.UtcNow;
            StartAsync((now + myDelay).Ticks, now);
          }
        }
      }).NoAwait(ourLogger);
      
      myLifetimes.TerminateCurrent();
    }

    // todo naming
    private void StartAsync(long nextUtcTimeTicks, DateTime startTime)
    {
      StartAsync(myLifetime, async () =>
      {
        while (myLifetime.IsAlive)
        {
          var lifetime = myLifetimes.Next();
          try
          {
            var shouldStop = await StartAsync(lifetime, nextUtcTimeTicks, startTime);
            if (shouldStop) return;
          }
          catch (Exception e) when (e.IsOperationCancelled())
          {
            if (myLifetime.IsNotAlive) return;
          }
          catch (Exception e)
          {
            ourLogger.Error(e, "Failed to execute action");
          }
          
          startTime = DateTime.UtcNow;
          nextUtcTimeTicks = (startTime + myDelay).Ticks;
          
          while (true)
          {
            var oldNextUtcTimeTicks = Interlocked.Read(ref myNextUtcTime);
            if (oldNextUtcTimeTicks >= nextUtcTimeTicks) return;
        
            if (Interlocked.CompareExchange(ref myNextUtcTime, nextUtcTimeTicks, oldNextUtcTimeTicks) == oldNextUtcTimeTicks)
              break;
          }
        }
      }).NoAwait(ourLogger);
    }

    private Task<bool> StartAsync(Lifetime lifetime, long expectedTicks, DateTime startTime)
    {
      return StartAsync(lifetime, async () =>
      {
        var nextDelay = myDelay;
        while (true)
        {
          await Task.Delay(nextDelay, lifetime);

          var currentNextUtcTimeTicks = Interlocked.Read(ref myNextUtcTime);
          if (currentNextUtcTimeTicks == 0) return true;

          var now = DateTime.UtcNow;
          if (currentNextUtcTimeTicks == expectedTicks || currentNextUtcTimeTicks <= now.Ticks || now - startTime >= myMaxDelay)
          {
            await ExecuteActionAsync(lifetime);

            var currentNextUtcTimeTicks2 = Interlocked.CompareExchange(ref myNextUtcTime, 0, currentNextUtcTimeTicks);
            return currentNextUtcTimeTicks == currentNextUtcTimeTicks2 || currentNextUtcTimeTicks2 == 0;
          }

          var nextUtcTime = new DateTime(currentNextUtcTimeTicks, DateTimeKind.Utc);
          if (nextUtcTime - startTime > myMaxDelay)
          {
            nextDelay = startTime + myMaxDelay - now;
          }
          else
          {
            nextDelay = nextUtcTime - now;
          }
        }
      });
    }

    private Task ExecuteActionAsync(Lifetime lifetime)
    {
      lifetime.ThrowIfNotAlive();
      
      return ourLogger.CatchAsync(async () =>
      {
        await myItemsProcessor.ProcessAsync(() =>
        {
          if (lifetime.IsAlive) myAction();
        });
      });
    }

    private static Task StartAsync(Lifetime lifetime, Action action, TaskScheduler? scheduler = null)
    {
      return StartAsync(lifetime, () =>
      {
        action();
        return Task.FromResult(Unit.Instance);
      }, scheduler);
    }
    
    private static Task<T> StartAsync<T>(Lifetime lifetime, Func<Task<T>> func, TaskScheduler? scheduler = null)
    {
      return Task.Factory.StartNew(func, lifetime, TaskCreationOptions.None, scheduler ?? TaskScheduler.Default).Unwrap();
    }
  }
}