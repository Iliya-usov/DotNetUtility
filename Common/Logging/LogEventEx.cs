using System;

namespace Common.Logging
{
  public static class LogEventEx
  {
    public static ReadOnlySpan<char> Present(in this LogEvent logEvent, ILogPresenter? presenter = null)
    {
      presenter ??= DefaultPresenter.Instance;
      return presenter.Present(logEvent);
    }
  }
}