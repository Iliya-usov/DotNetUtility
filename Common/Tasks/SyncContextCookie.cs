using System;
using System.Threading;

namespace Common.Tasks
{
  public readonly struct SyncContextCookie : IDisposable
  {
    private readonly SynchronizationContext? myOldContext;

    public SyncContextCookie(SynchronizationContext newContext)
    {
      myOldContext = SynchronizationContext.Current;
      SynchronizationContext.SetSynchronizationContext(newContext);
    }

    public void Dispose() => SynchronizationContext.SetSynchronizationContext(myOldContext);
  }
}