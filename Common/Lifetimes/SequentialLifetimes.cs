using System.Threading;

namespace Common.Lifetimes
{
  public class SequentialLifetimes
  {
    public static SequentialLifetimes Terminated { get; } = new SequentialLifetimes(Lifetime.Terminated);
    
    private readonly Lifetime myParent;
    private LifetimeDefinition? myCurrentLifetime;
    
    public SequentialLifetimes(Lifetime parent = default)
    {
      myParent = parent;
    }
    
    // todo reentrancy?
    public Lifetime Next()
    {
      if (myParent.IsNotAlive) return Lifetime.Terminated;

      var newDef = myParent.DefineNested();
      if (newDef.IsNotAlive) return Lifetime.Terminated;

      var oldDef = myCurrentLifetime;
      while (Interlocked.CompareExchange(ref myCurrentLifetime, newDef, oldDef) != oldDef)
        oldDef = myCurrentLifetime;

      oldDef?.Terminate();
      return newDef.Lifetime;
    }

    public void TerminateCurrent() => myCurrentLifetime?.Terminate();
  }
}