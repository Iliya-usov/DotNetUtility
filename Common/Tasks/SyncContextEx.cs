using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Common.Tasks
{
  public static class SyncContextEx
  {
    [MustUseReturnValue]
    public static SyncContextCookie Cookie(this SynchronizationContext context) => new SyncContextCookie(context);
    public static SynchronizationContext CreateSynchronizationContext(this TaskScheduler scheduler) => new SchedulerSyncContext(scheduler);
  }
}