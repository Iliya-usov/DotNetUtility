using System;
using System.Threading;
using Common.Logging;

namespace Common.Disposing
{
  public abstract class SafeDisposable : IDisposable
  {
    protected static ILogger Logger { get; } = Logging.Logger.GetLogger<SafeDisposable>();
    private int myDisposed;

    ~SafeDisposable()
    {
      if (Volatile.Read(ref myDisposed) != 0)
        return; 
      
      Logger.Error($"Memory leak detected. Object {GetType()} must be disposed");
      Logger.Catch(Dispose);
    }

    public void Dispose()
    {
      if (Interlocked.CompareExchange(ref myDisposed, 1, 0) != 0)
      {
        Logger.Error("Disposed twice");
        return;
      }

      DisposeUnmanagedResources();
      GC.SuppressFinalize(this);
    }

    protected abstract void DisposeUnmanagedResources();
  }
}