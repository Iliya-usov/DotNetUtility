using System;

namespace Common.Logging
{
  public class DefaultPresenter : ILogPresenter
  {
    public static ILogPresenter Instance { get; } = new DefaultPresenter();
    
    public ReadOnlySpan<char> Present(in LogEvent logEvent)
    {
      var errorKind = logEvent.Level switch
      {
        LoggingLevel.None => 'N',
        LoggingLevel.Error => 'E',
        LoggingLevel.Warn => 'W',
        LoggingLevel.Info => 'I',
        LoggingLevel.Verbose => 'V',
        LoggingLevel.Trace => 'T',
        _ => throw new ArgumentOutOfRangeException($"Unknown logEvenr: {logEvent}")
      };
      
      if (logEvent.Exception != null)
        return $"{logEvent.DateTime:HH:mm:ss} |{errorKind}| {logEvent.Category} | {logEvent.Message}. Exception: {logEvent.Exception}";
      
      return $"{logEvent.DateTime:HH:mm:ss} |{errorKind}| {logEvent.Category} | {logEvent.Message}.";
    }
  }
}