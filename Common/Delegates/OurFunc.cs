using System;
using System.Threading;

namespace Common.Delegates
{
  public static class OurFunc
  {
    public static Func<bool> True { get; } = () => true;
    public static Func<bool> False { get; } = () => false;
    
    public static Func<CancellationToken> None { get; }=  () => CancellationToken.None;
  }
  
  public static class OurFunc<T>
  {
    public static Func<T, bool> True { get; } = _ => true;
    public static Func<T, bool> False { get; } = _ => false;

    public static Func<T, bool> Null { get; } = x => x == null;
    public static Func<T, bool> NotNull { get; } = x => x != null;
    
    public static Func<T, CancellationToken> None { get; }=  _ => CancellationToken.None;
  }
}