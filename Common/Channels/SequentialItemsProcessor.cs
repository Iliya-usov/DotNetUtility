using System;
using System.Threading.Tasks;
using Common.DataStructures;
using Common.Extensions;
using Common.Lifetimes;
using JetBrains.Annotations;

namespace Common.Channels
{
  // todo tests
  public class SequentialItemsProcessor<T>
  {
    private readonly Lifetime myLifetime;
    private readonly Actor<Func<Task>> myActor;

    public SequentialItemsProcessor(Lifetime lifetime, [NotNull] string id, [CanBeNull] TaskScheduler? scheduler = null)
    {
      myLifetime = lifetime;
      myActor = new Actor<Func<Task>>(lifetime, id, action => action(), scheduler);
    }

    public Task<T> ProcessAsync(Func<Task<T>> func)
    {
      var tcs = myLifetime.CreateTaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      // no need to await SendAsync, because caller will await ProcessAsync
      myActor.SendAsync(async () =>
      {
        try
        {
          var result = await func().ConfigureAwait(false);
          tcs.TrySetResult(result);
        }
        catch (Exception e) when (e.IsOperationCancelled())
        {
          tcs.TrySetCanceled();
        }
        catch (Exception e)
        {
          tcs.TrySetException(e);
        }
      });
      return tcs.Task;
    }
    
    public Task<T> ProcessAsync(Func<T> func) => ProcessAsync(() => Task.FromResult(func()));
  }

  public class SequentialItemsProcessor : SequentialItemsProcessor<Unit>
  {
    public SequentialItemsProcessor(Lifetime lifetime, [NotNull] string id, [CanBeNull] TaskScheduler? scheduler = null) : base(lifetime, id, scheduler)
    {
    }
    
    public Task ProcessAsync(Action action)
    {
      return ProcessAsync(() =>
      {
        action();
        return Unit.Instance;
      });
    }
  } 
}