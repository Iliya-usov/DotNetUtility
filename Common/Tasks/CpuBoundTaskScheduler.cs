using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Concurrent.Collections;
using Common.Extensions;
using Common.Lifetimes;
using Common.Logging;

namespace Common.Tasks
{
  public class CpuBoundTaskScheduler : TaskScheduler
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<CpuBoundTaskScheduler>();

    private static readonly Lazy<CpuBoundTaskScheduler> ourLazyDefaultScheduler = new Lazy<CpuBoundTaskScheduler>(() => new CpuBoundTaskScheduler(Lifetime.Eternal, Environment.ProcessorCount));
    public new static CpuBoundTaskScheduler Default => ourLazyDefaultScheduler.Value;
    
    [ThreadStatic]
    private static bool ourIsActiveThread;
    
    private readonly BlockingQueue<Action> myQueue;

    private readonly Lifetime myLifetime;
    private readonly SynchronizationContext mySyncContext;

    public CpuBoundTaskScheduler(Lifetime lifetime, int threadsCount)
    {
      myLifetime = lifetime;

      mySyncContext = this.CreateSynchronizationContext();
      myQueue = BlockingCollections.Queue<Action>();

      for (var i = 0; i < threadsCount; i++) 
        StartWorker(i);
      
      myLifetime.OnTermination(FlushQueueAsync);
    }

    protected override void QueueTask(Task task)
    {
      using var _ = myLifetime.UsingExecuteIfAliveOrThrow();
      myQueue.Enqueue(() => TryExecuteWithContext(task));
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      return ourIsActiveThread && TryExecuteWithContext(task);
    }

    private bool TryExecuteWithContext(Task task)
    {
      using (mySyncContext.Cookie())
        return TryExecuteTask(task);
    }
    
    private void StartWorker(int id)
    {
      new Thread(() =>
      {
        ourIsActiveThread = true;
        while (myLifetime.IsAlive)
        {
          try
          {
            var action = myQueue.DequeueOrBlock();
            action();
          }
          catch (Exception e) when (e.IsOperationCancelled())
          {
          }
          catch (Exception e)
          {
            ourLogger.Error(e, "Failed to execute action");
          }
        }
      }) {IsBackground = true, Name = $"Worker Thread {id}"}.Start();
    }

    private void FlushQueueAsync()
    {
      if (myQueue.Count == 0)
      {
        myQueue.Dispose();
        return;
      }

      new Thread(() =>
      {
        while (myQueue.TryDequeue(out var action))
        {
          try
          {
            action();
          }
          catch (Exception e)
          {
            ourLogger.Error(e, "Failed to execute action");
          }
        }

        myQueue.Dispose();
      }) {IsBackground = true, Name = "Flush thread", Priority = ThreadPriority.BelowNormal}.Start();
    }
    
    protected override IEnumerable<Task>? GetScheduledTasks() => null;
  }
}