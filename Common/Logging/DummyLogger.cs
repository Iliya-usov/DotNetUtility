using System;

namespace Common.Logging
{
  public class DummyLogger : ILogger
  {
    public static DummyLogger Instance { get; } = new DummyLogger();
    
    public LoggingLevel CurrentLevel => LoggingLevel.None;
    public void Log(LoggingLevel level, string message) { }
    public void LogException(LoggingLevel level, Exception e, string? message) { }
  }
}