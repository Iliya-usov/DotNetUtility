namespace Common.Lifetimes
{
  public enum LifetimeStatus : byte
  {
    Alive,
    Cancelling,
    Terminating,
    Terminated
  }
}