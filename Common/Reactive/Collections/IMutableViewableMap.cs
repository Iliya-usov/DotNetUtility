namespace Common.Reactive.Collections
{
  public interface IMutableViewableMap<TKey, TValue> : IViewableMap<TKey, TValue> where TKey : notnull
  {
    void Add(TKey key, TValue value);
    bool Remove(TKey key);

    new TValue this[TKey key] { get; set; }
  }
}