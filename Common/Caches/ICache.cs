namespace Common.Caches
{
  public interface ICache<in TKey, TValue> where TKey : notnull
  {
    int Capacity { get; }
    bool IsFixedSize { get; }

    void AddOrUpdate(TKey key, TValue value);
    bool TryGetValue(TKey key, out TValue value);
    bool Remove(TKey key);
  }
}