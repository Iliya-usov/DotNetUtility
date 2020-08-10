using System;

namespace Common.Logging
{
  public interface ILogger
  {
    LoggingLevel CurrentLevel { get; }
    void Log(LoggingLevel level, string message);
    void LogException(LoggingLevel level, Exception e, string? message = null);
  }
}