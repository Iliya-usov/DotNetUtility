using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common.Lifetimes;

namespace Common.ProcessUtil
{
  public static class ProcessEx
  {
    public static async Task<int?> WaitForProcessExitAsync(this Process process, TimeSpan timeSpan, Lifetime lifetime = default)
    {
      var task = process.WaitForProcessExitAsync(lifetime);
      await Task.WhenAny(task, Task.Delay(timeSpan)).ConfigureAwait(false);
      
      return task.IsCompleted ? task.Result : (int?) null;
    }

    public static Task<int> WaitForProcessExitAsync(this Process process, Lifetime lifetime = default)
    {
      var tcs = lifetime.CreateTaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

      process.EnableRaisingEvents = true;
      process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);

      if (process.HasExited)
        tcs.TrySetResult(process.ExitCode);

      return tcs.Task;
    }
  }
}