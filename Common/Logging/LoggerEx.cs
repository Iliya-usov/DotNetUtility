using System;
using System.Threading.Tasks;
using Common.Extensions;

namespace Common.Logging
{
  public static class LoggerEx
  {  
    public static void Error(this ILogger logger, string message)
    {
      if (logger.CurrentLevel > LoggingLevel.Error) return;
      logger.Log(LoggingLevel.Error, message);
    }

    public static void Error(this ILogger logger, Exception e, string? message = null)
    {
      if (logger.CurrentLevel > LoggingLevel.Error) return;
      logger.LogException(LoggingLevel.Error, e, message);
    }
    
    public static void Warn(this ILogger logger, string message)
    {
      if (logger.CurrentLevel > LoggingLevel.Warn) return;
      logger.Log(LoggingLevel.Warn, message);
    }
    
    public static void Info(this ILogger logger, string message)
    {
      if (logger.CurrentLevel > LoggingLevel.Info) return;
      logger.Log(LoggingLevel.Warn, message);
    }
    
    public static void Verbose(this ILogger logger, string message)
    {
      if (logger.CurrentLevel > LoggingLevel.Verbose) return;
      logger.Log(LoggingLevel.Warn, message);
    }
    
    public static void Trace(this ILogger logger, string message)
    {
      if (logger.CurrentLevel > LoggingLevel.Trace) return;
      logger.Log(LoggingLevel.Warn, message);
    }

    public static void Catch(this ILogger logger, Action action)
    {
      try
      {
        action();
      }  
      catch (Exception e) when (e.IsOperationCancelled())
      {
        throw; // todo ?
      }
      catch (Exception e)
      {
        logger.Error(e);
      }
    } 
    
    public static async Task CatchAsync(this ILogger logger, Func<Task> action)
    {
      try
      {
        await action().ConfigureAwait(false);
      }
      catch (Exception e) when (e.IsOperationCancelled())
      {
        throw; // todo ?
      }
      catch (Exception e)
      {
        logger.Error(e);
      }
    } 
  }
}