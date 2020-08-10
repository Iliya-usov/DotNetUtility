using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TestUtils
{
  public static class ConcurrentTestUtil
  {
    public static void ParallelInvoke(int threadCount, Action action, TimeSpan timeout) => ParallelInvokeAsync(threadCount, action).Wait(timeout);
    public static void ParallelInvoke(int threadCount, Action<int> action, TimeSpan timeout) => ParallelInvokeAsync(threadCount, action).Wait(timeout);
    
    public static Task ParallelInvokeAsync(int threadCount, Action action) => ParallelInvokeAsync(threadCount, _ => action());
    public static Task ParallelInvokeAsync(int threadCount, Action<int> action)
    {
      var i = 0;
      var tasks = Enumerable.Range(0, threadCount)
        .Select(j => Task.Factory.StartNew(() =>
        {
          Interlocked.Increment(ref i);
          SpinWait.SpinUntil(() => threadCount == i);
          action(j);
        }, TaskCreationOptions.LongRunning /*new thread*/))
        .ToArray();

      return Task.WhenAll(tasks);
    }

    public static void SpinUntilOrThrowIfTimeout([InstantHandle] Func<bool> condition, TimeSpan timeout, string? message = null)
    {
      var success = SpinWait.SpinUntil(condition, timeout);
      if (success) return;
      
      throw new TimeoutException($"Timeout: {timeout}. Message = " + message);
    }
  }
}