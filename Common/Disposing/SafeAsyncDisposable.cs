using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Logging;

namespace Common.Disposing
{
  public abstract class SafeAsyncDisposable : IAsyncDisposable
  {
    protected static ILogger Logger { get; } = Logging.Logger.GetLogger<SafeAsyncDisposable>();
    
    private int myDisposed;

    ~SafeAsyncDisposable()
    {
      if (Volatile.Read(ref myDisposed) != 0)
        return; 
      
      Logger.Error($"Memory leak detected. Object {GetType()} must be disposed");
      DisposeAsync().NoAwait(Logger);
    }

    public async ValueTask DisposeAsync()
    {
      if (Interlocked.CompareExchange(ref myDisposed, 1, 0) != 0)
      {
        Logger.Error("Disposed twice");
        return;
      }

      await DisposeUnmanagedResources().ConfigureAwait(false);
      GC.SuppressFinalize(this);
    }

    protected abstract ValueTask DisposeUnmanagedResources();
  }
}