using System;

namespace Common.Caches
{
  public static class CacheEx
  {
    public static TValue GetOrAdd<TKey, TValue>(this ICache<TKey, TValue> cache, TKey key, Func<TKey, TValue> factory)
      where TKey : notnull
    {
      if (cache.TryGetValue(key, out var value))
        return value;

      value = factory(key);
      cache.AddOrUpdate(key, value);
      return value;
    }

    public static SynchronizedCache<TKey, TValue> ToSynchronized<TKey, TValue>(this ICache<TKey, TValue> cache)
      where TKey : notnull
    {
      if (cache is SynchronizedCache<TKey, TValue> synchronizedCache)
        return synchronizedCache;

      return new SynchronizedCache<TKey, TValue>(cache);
    }
  }
}