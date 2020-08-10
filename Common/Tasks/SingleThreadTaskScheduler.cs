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
  public class SingleThreadTaskScheduler : TaskScheduler
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<SingleThreadTaskScheduler>();

    [ThreadStatic]
    private static bool ourIsActiveThread;
    
    private readonly Lifetime myLifetime;
    private readonly SynchronizationContext mySyncContext;

    private readonly Lazy<Executor> myLazyExecutor;

    public SingleThreadTaskScheduler(Lifetime lifetime, string id, ThreadPriority priority = ThreadPriority.Normal)
    {
      myLifetime = lifetime;
      mySyncContext = this.CreateSynchronizationContext();
      myLazyExecutor = new Lazy<Executor>(() => new Executor(priority, id));
      
      lifetime.OnTermination(() =>
      {
        if (myLazyExecutor.IsValueCreated)
          myLazyExecutor.Value.Enqueue(() => myLazyExecutor.Value.Dispose());
      });
    }

    protected override void QueueTask(Task task)
    {
      using var _ = myLifetime.UsingExecuteIfAliveOrThrow();
      myLazyExecutor.Value.Enqueue(() => TryExecuteWithContext(task));
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      return ourIsActiveThread && myLifetime.IsAlive && TryExecuteWithContext(task);
    }

    private bool TryExecuteWithContext(Task task)
    {
      using (mySyncContext.Cookie())
        return TryExecuteTask(task);
    }
    
    protected override IEnumerable<Task>? GetScheduledTasks() => null;

    private class Executor : IDisposable
    {
      private readonly BlockingQueue<Action> myQueue;

      public Executor(ThreadPriority priority, string id)
      {
        myQueue = BlockingCollections.Queue<Action>();
        
        new Thread(() =>
        {
          ourIsActiveThread = true;
          while (myQueue.IsAlive)
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
        }) {IsBackground = true, Priority = priority, Name = $"Single Thread Executor: {id}"}.Start();
      }

      public void Enqueue(Action action) => myQueue.Enqueue(action);

      public void Dispose() => myQueue.Dispose();
    }
  }
}