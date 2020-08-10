using System;

namespace Common.Logging
{
  public interface ILogPresenter
  {
    public ReadOnlySpan<char> Present(in LogEvent logEvent);
  }
}