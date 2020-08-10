using System;

namespace Common.Logging
{
  public static class Logger
  {
    public static ILogger GetLogger<T>() => GetLogger(typeof(T));
    public static ILogger GetLogger(Type type) => GetLogger(type.FullName ?? "<NULL>");
    public static ILogger GetLogger(string category) => LogManager.Instance.CreateLogger(category);
  }
}