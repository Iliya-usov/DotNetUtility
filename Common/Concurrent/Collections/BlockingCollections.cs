namespace Common.Concurrent.Collections
{
  public static class BlockingCollections
  {
    public static BlockingQueue<T> Queue<T>() => new BlockingQueue<T>();
    public static BlockingPriorityQueue<T> PriorityQueue<T>() => new BlockingPriorityQueue<T>();
    public static BlockingStack<T> Stack<T>() => new BlockingStack<T>();
  }
}