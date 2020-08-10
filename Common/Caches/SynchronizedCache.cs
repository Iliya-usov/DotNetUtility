namespace Common.Caches
{
  public class SynchronizedCache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
  {
    private readonly ICache<TKey, TValue> myCache;
    private readonly object myLock = new object();

    // ReSharper disable once InconsistentlySynchronizedField
    public int Capacity => myCache.Capacity;
    // ReSharper disable once InconsistentlySynchronizedField
    public bool IsFixedSize => myCache.IsFixedSize;

    public SynchronizedCache(ICache<TKey, TValue> cache)
    {
      myCache = cache;
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
      lock (myLock) myCache.AddOrUpdate(key, value);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      lock (myLock) return myCache.TryGetValue(key, out value);
    }

    public bool Remove(TKey key)
    {
      lock (myLock) return myCache.Remove(key);
    }
  }
}