namespace Common.Collections
{
  public class EmptyArray<T>
  {
    public static T[] Instance { get; } = new T[0];
  }
}