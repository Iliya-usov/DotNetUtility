using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Common.Caches.Timestamped;
using Common.DataStructures;
using Common.Delegates;
using Common.Extensions;

namespace Common.Logging
{
  public class LogManager
  {
    public static LogManager Instance { get; } = new LogManager();
    
    private ImmutableArray<ILogListener> myListeners = ImmutableArray<ILogListener>.Empty;

    private LogInfo myInfo = LogInfo.Default;
    private int myTimestamp = TimestampedCache.InitTimestamp;
    
    public ILogger CreateLogger(string category)
    {
      var level = new TimestampedCache<LoggingLevel>(() => GetCurrentLogLevel(category), () => myTimestamp);
      return new DefaultLogger(level, category, Fire);
    }

    public LoggingLevel GetCurrentLogLevel(string category)
    {
      var logInfo = myInfo; // todo check generic classes
      
      var traceCategories = logInfo.TraceCategories;
      if (traceCategories == null || category.Length == 0) return logInfo.DefaultLogLevel; // todo ext method is null or empty

      if (category.IsNullOrEmpty()) return LoggingLevel.Trace; // todo ext method
      
      var length = category.Length;
      while (category.IsNotEmpty())
      {
        var slice = new StringSlice(category, 0, length);
        if (traceCategories.Contains(slice))
          return LoggingLevel.Trace;

        length = slice.Value.LastIndexOfAny('.', '+');
        if (length == -1) break;
      }

      return logInfo.DefaultLogLevel;
    }

    public void Apply(IReadOnlyCollection<string> traceCategories, LoggingLevel defaultLevel = LoggingLevel.Info)
    {
      myInfo = new LogInfo(traceCategories.Select(x => new StringSlice(x)).ToHashSet(StringSlice.EqualityComparer), defaultLevel);
      TimestampedCache.IncrementTimestamp(ref myTimestamp);
    }

    public void AddListeners(params ILogListener[] listeners)
    {
      lock (this) myListeners = myListeners.AddRange(listeners); // todo interlocked ? 
    }
    
    public void RemoveListeners(params ILogListener[] listeners)
    {
      lock (this) myListeners = myListeners.RemoveRange(listeners); // todo interlocked ? 
    }

    private class LogInfo
    {
      public static LogInfo Default { get; } = new LogInfo(null, LoggingLevel.Info); 
      
      public HashSet<StringSlice>? TraceCategories { get; }
      public LoggingLevel DefaultLogLevel { get; }

      public LogInfo(HashSet<StringSlice>? traceCategories, LoggingLevel defaultLogLevel)
      {
        TraceCategories = traceCategories;
        DefaultLogLevel = defaultLogLevel;
      }
    }

    private void Fire(in LogEvent @event)
    {
      // ReSharper disable once InconsistentlySynchronizedField
      foreach (var listener in myListeners)
      {
        try
        {
          listener.OnLog(@event);
        }
        catch
        {
          // todo error
        }
      }
    }
    
    private class DefaultLogger : ILogger
    {
      private readonly TimestampedCache<LoggingLevel> myTimestampedLevel;
      private readonly string myCategory;
      private readonly ActionIn<LogEvent> myOnLog;

      public LoggingLevel CurrentLevel => myTimestampedLevel.GetValue();

      public DefaultLogger(
        TimestampedCache<LoggingLevel> timestampedLevel, 
        string category,
        ActionIn<LogEvent> onLog)
      {
        myTimestampedLevel = timestampedLevel;
        myCategory = category;
        myOnLog = onLog;
      }
      
      public void Log(LoggingLevel level, string message)
      {
        myOnLog(LogEvent.Create(level, message, myCategory));
      }

      public void LogException(LoggingLevel level, Exception e, string? message = null)
      {
        myOnLog(LogEvent.Create(level, message, myCategory, e));
      }
    }
  }
}