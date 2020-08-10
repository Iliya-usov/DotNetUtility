using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Concurrent.Collections;
using Common.Extensions;
using Common.FileSystem;

namespace Common.Logging
{
  public class AsyncLogProcessor : ILogListener, IDisposable, IAsyncDisposable
  {
    private readonly Task myTask;
    private readonly TextWriter myTextWriter;
    private readonly ILogPresenter myPresenter;
    private readonly BlockingQueue<LogEvent> myEvents;
    
    private int myDisposed;

    public AsyncLogProcessor(FileSystemPath fileName, ILogPresenter presenter)
    {
      myPresenter = presenter;
      myTextWriter = new TextWriter(fileName);
      myEvents = BlockingCollections.Queue<LogEvent>();
      myTask = Task.Factory.StartNew(Process, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void OnLog(in LogEvent logEvent) => myEvents.Enqueue(logEvent);

    private void Process()
    {
      while (myEvents.IsAlive)
      {
        try
        {
          var logEvent = myEvents.DequeueOrBlock();
          var presentation = logEvent.Present(myPresenter); // todo less allocations in presentation
          myTextWriter.Write(presentation);
        }
        catch (Exception e) when (e.IsOperationCancelled())
        {
          return;
        }
        catch (ObjectDisposedException)
        {
          return;
        }
        catch (Exception e)
        {
          Console.WriteLine($"Failed to process logEvent with error: {e}");
        }
      }
    }

    public void Dispose()
    {
      if (!TryChangeToDispose()) return;
      myEvents.Dispose();

      myTask.Wait();

      myTextWriter.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
      if (!TryChangeToDispose()) return;

      myEvents.Dispose();

      await myTask.ConfigureAwait(false);
      await myTextWriter.DisposeAsync().ConfigureAwait(false);
    }

    private bool TryChangeToDispose()
    {
      return Interlocked.CompareExchange(ref myDisposed, 1, 0) == 0;
    }
  }
}