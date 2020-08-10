using System;
using System.Threading.Tasks;

namespace Common.Tasks
{
  public static class TaskUtil
  {
    public static Task CheckForInterruptAsync(Func<bool> isCancelled, int maxDelay = 64, bool continueOnCapturedContext = false)
    {
      return WaitForInterruptAsync(() =>
      {
        if (isCancelled())
          throw new OperationCanceledException();

        return false;
      }, maxDelay, continueOnCapturedContext);
    }
    
    public static async Task WaitForInterruptAsync(Func<bool> isCancelled, int maxDelay = 64, bool continueOnCapturedContext = false)
    {
      var ms = 1;
      while (!isCancelled())
      {
        await Task.Delay(ms).ConfigureAwait(continueOnCapturedContext);
        
        if (ms < maxDelay)
        {
          ms *= 2;
          if (ms > maxDelay)
            ms = maxDelay;
        }
      }
    }
  }
}