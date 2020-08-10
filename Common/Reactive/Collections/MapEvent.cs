namespace Common.Reactive.Collections
{
  public static class MapEvent
  {
    public static MapEvent<TKey, TValue> Add<TKey, TValue>(TKey key, TValue value) => new MapEvent<TKey, TValue>(MapEventKind.Add, key, value);
    public static MapEvent<TKey, TValue> Remove<TKey, TValue>(TKey key, TValue value) => new MapEvent<TKey, TValue>(MapEventKind.Remove, key, value);
    public static MapEvent<TKey, TValue> Update<TKey, TValue>(TKey key, TValue value) => new MapEvent<TKey, TValue>(MapEventKind.Update, key, value);
  }

  public readonly struct MapEvent<TKey, TValue>
  {
    public MapEventKind Kind { get; }
    public TKey Key { get; }
    public TValue Value { get; }

    public MapEvent(MapEventKind kind, TKey key, TValue value)
    {
      Kind = kind;
      Key = key;
      Value = value;
    }
  }
}