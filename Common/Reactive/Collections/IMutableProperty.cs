namespace Common.Reactive.Collections
{
  public interface IMutableProperty<T> : IProperty<T>
  {
    public void SetValue(T value);
  }
}