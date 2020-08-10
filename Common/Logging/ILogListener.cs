namespace Common.Logging
{
  public interface ILogListener
  {
    void OnLog(in LogEvent logEvent);
  }
}