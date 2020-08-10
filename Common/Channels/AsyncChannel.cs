using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Lifetimes;
using Common.Logging;

namespace Common.Channels
{
  // todo tests
  // todo buffer
  public class AsyncChannel<T> : IAsyncReceiver<T>, IAsyncSender<T>
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<AsyncChannel<T>>();

    private readonly Lifetime myLifetime;
    private readonly Queue<T> myMessages = new Queue<T>();
    private readonly Queue<TaskCompletionSource<T>> myExpectedReceiveTasks = new Queue<TaskCompletionSource<T>>();

    private readonly object myLock = new object();

    public int Count => myMessages.Count;

    public AsyncChannel(Lifetime lifetime, string id)
    {
      myLifetime = lifetime;
      lifetime.OnTermination(() =>
      {
        lock (myLock) { } // just sync

        foreach (var tcs in myExpectedReceiveTasks) 
          tcs.TrySetCanceled();

        if (myMessages.Count == 0) return;
        
        ourLogger.Error($"Has no received messages in channel: {id}"); // todo text
        myMessages.Clear();
      });
    }
    
    public Task<T> ReceiveAsync()
    {
      lock (myLock)
      {
        if (myLifetime.IsNotAlive)
          return Task.FromCanceled<T>(myLifetime); // todo cache?

        if (myMessages.IsEmpty())
        {
          var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
          myExpectedReceiveTasks.Enqueue(tcs);
          return tcs.Task;
        }

        var message = myMessages.Dequeue();
        return Task.FromResult(message);
      }
    }

    public Task SendAsync(T message)
    {
      lock (myLock) // todo add max buffer size
      {
        myLifetime.ThrowIfNotAlive();

        if (myExpectedReceiveTasks.IsEmpty()) myMessages.Enqueue(message);
        else myExpectedReceiveTasks.Dequeue().TrySetResult(message);
      }
      
      return Task.CompletedTask;
    }
  }
}