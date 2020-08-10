using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Channels;
using Common.Lifetimes;

namespace Common.Tasks
{
  public class SequentialTaskScheduler : TaskScheduler
  {
    private readonly Lifetime myLifetime;
    private readonly Actor<Action> myActor;
    private readonly SynchronizationContext mySyncContext;

    public SequentialTaskScheduler(Lifetime lifetime, string id, TaskScheduler? scheduler = null)
    {
      myLifetime = lifetime;
      mySyncContext = this.CreateSynchronizationContext();
      var def = new LifetimeDefinition();
      myActor = new Actor<Action>(def.Lifetime, id, action => action(), scheduler);
      lifetime.OnTermination(() => myActor.SendAsync(() => def.Terminate()));
    }

    protected override void QueueTask(Task task)
    {
      using var _ = myLifetime.UsingExecuteIfAliveOrThrow();
      myActor.SendAsync(() =>
      {
        using (mySyncContext.Cookie()) 
          TryExecuteTask(task);
      });
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    protected override IEnumerable<Task>? GetScheduledTasks() => null;
  }
}