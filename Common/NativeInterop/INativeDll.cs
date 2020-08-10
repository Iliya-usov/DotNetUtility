using System;

namespace Common.NativeInterop
{
  public interface INativeDll
  {
    T ImportMethod<T>(string name) where T : Delegate;
    Delegate ImportMethod(string name, Type type);

    /// <summary>
    /// T must be is interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Import<T>() where T : class;
  }
}