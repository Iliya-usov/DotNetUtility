using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Tasks
{
  public class SchedulerSyncContext : SynchronizationContext
  {
    private readonly TaskScheduler myScheduler;

    public SchedulerSyncContext(TaskScheduler scheduler)
    {
      myScheduler = scheduler;
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
      var action = new Action<object?>(d);
      Task.Factory.StartNew(action, state, CancellationToken.None, TaskCreationOptions.None, myScheduler);
    }
  }
}