namespace Common.Reactive.Collections
{
  public interface ISignal<T> : ISource<T>
  {
    public void Fire(T value);
  }
}