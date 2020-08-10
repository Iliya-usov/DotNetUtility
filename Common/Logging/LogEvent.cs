using System;

namespace Common.Logging
{
  public readonly struct LogEvent
  {
    public LoggingLevel Level { get; }
    public string Message { get; }
    public string Category { get; }
    public Exception? Exception { get; }
    public DateTime DateTime { get; }
    
    private LogEvent(LoggingLevel level, string message, string category, Exception? e)
    {
      Level = level;
      Message = message;
      Category = category;
      Exception = e;
      DateTime = DateTime.UtcNow;
    }

    public static LogEvent Create(LoggingLevel level, string message, string category)
    {
      return new LogEvent(level, message, category, null);
    }
    
    public static LogEvent Create(LoggingLevel level, string? message, string category, Exception e)
    {
      return new LogEvent(level, message ?? e.Message, category, e);
    }
  }
}