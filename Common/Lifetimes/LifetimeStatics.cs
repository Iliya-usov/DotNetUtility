using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Lifetimes
{
  public readonly partial struct Lifetime
  {
    public static Lifetime Eternal => new Lifetime();
    public static Lifetime Terminated => new Lifetime(LifetimeDefinition.Terminated);
    
    public static Lifetime Intersect(params Lifetime[] lifetimes) => DefineIntersection(lifetimes).Lifetime;

    public static LifetimeDefinition DefineIntersection(params Lifetime[] lifetimes)
    {
      var def = new LifetimeDefinition();
      foreach (var lifetime in lifetimes)
      {
        lifetime.Def?.AttachOrTerminate(def);
        if (def.IsNotAlive) return def;
      }

      return def;
    }
    
    public static void Using(Action<Lifetime> action)
    {
      using var def = new LifetimeDefinition();
      action(def.Lifetime);
    }

    public static T Using<T>(Func<Lifetime, T> action)
    {
      using var def = new LifetimeDefinition();
      return action(def.Lifetime);
    }

    public static async Task UsingAsync(Func<Lifetime, Task> action)
    {
      using var def = new LifetimeDefinition();
      await action(def.Lifetime);
    }

    public static async Task<T> UsingAsync<T>(Func<Lifetime, Task<T>> action)
    {
      using var def = new LifetimeDefinition();
      return await action(def.Lifetime);
    }

    public class Comparer : IEqualityComparer<Lifetime>
    {
      public static Comparer Instance { get; } = new Comparer();
      
      public bool Equals(Lifetime x, Lifetime y) => x.Equals(y);
      public int GetHashCode(Lifetime obj) => obj.GetHashCode();
    }
  }
}