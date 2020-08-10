using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Common.Reflection;

namespace Common.Extensions
{
  public static class TaskEx
  {
    private static readonly ILogger ourLogger = Logger.GetLogger(typeof(TaskEx));
    
    public static CancellationToken GetCancellationToken(this Task task)
    {
      return TaskUtil.GetCancellationToken(task);
    }

    public static bool IsCancelled(this Task task)
    {
      return task.IsCompleted && (task.IsCanceled || (task.Exception?.IsOperationCancelled() ?? false));
    }

    public static void NoAwait(this ValueTask task, ILogger? logger = null) => task.AsTask().NoAwait(logger);
    public static void NoAwait(this Task task, ILogger? logger = null)
    {
      logger ??= ourLogger;
      task.ContinueWith(t =>
      {
        if (t.Exception != null)
          logger.Error($"Task is faulted. Exception: {t.Exception}");
      }, TaskContinuationOptions.ExecuteSynchronously);
    }
  }
}