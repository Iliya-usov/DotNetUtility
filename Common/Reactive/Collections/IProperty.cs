using System.Diagnostics.CodeAnalysis;

namespace Common.Reactive.Collections
{
  public interface IProperty<out T> : ISource<T>
  {
    public ISource<T> Change { get; }
    public bool HasValue { get; }
    
    [AllowNull]
    T ValueOrDefault { get; }
    
    T ValueOrThrow { get; }
  }
}