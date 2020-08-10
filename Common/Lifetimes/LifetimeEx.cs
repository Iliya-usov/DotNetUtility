using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Common.DataStructures;
using Common.Monads;

namespace Common.Lifetimes
{
  public static class LifetimeEx
  {
    public static LifetimeDefinition DefineNested(this Lifetime lifetime)
    {
      return lifetime.IsAlive ? new LifetimeDefinition(lifetime) : LifetimeDefinition.Terminated;
    }

    public static SequentialLifetimes DefineNestedSequential(this Lifetime lifetime)
    {
      return lifetime.IsAlive ? new SequentialLifetimes(lifetime) : SequentialLifetimes.Terminated;
    }

    public static T WithLifetime<T>(this T value, Lifetime lifetime) where T : IDisposable
    {
      lifetime.OnTermination(value);
      return value;
    }

    public static LifetimeDefinition DefineIntersect(this Lifetime lifetime, Lifetime other)
    {
      if (lifetime.IsNotAlive || other.IsNotAlive) 
        return LifetimeDefinition.Terminated;

      var def = new LifetimeDefinition();
      lifetime.Def?.AttachOrTerminate(def);
      other.Def?.AttachOrTerminate(def);
      return def;
    }

    public static Lifetime Intersect(this Lifetime lifetime, Lifetime other)
    {
      if (lifetime.IsEternal) return other;
      if (other.IsEternal) return lifetime;
      return lifetime.DefineIntersect(other).Lifetime;
    }
    
    public static bool TryExecute(this Lifetime lifetime, Action action)
    {
      using var cookie = lifetime.UsingExecuteIfAlive();
      if (!cookie.Success) return false;
      
      action();
      return true;
    }

    public static Result<T> TryExecute<T>(this Lifetime lifetime, Func<T> action)
    {
      using var cookie = lifetime.UsingExecuteIfAlive();
      return cookie.Success ? Result.Create(action()) : Result<T>.Unsuccess;
    }
    
    public static void Execute(this Lifetime lifetime, Action action)
    {
      if (lifetime.TryExecute(action)) return;
      throw new LifetimeCanceledException(lifetime);
    }

    [return: MaybeNull]
    public static T Execute<T>(this Lifetime lifetime, Func<T> action)
    {
      var result = lifetime.TryExecute(action);
      return result.Success ? result.Value : throw new LifetimeCanceledException(lifetime);
    }
    
    public static Result<T> TryBracket<T>(this Lifetime lifetime, Func<T> open, Action<T> close)
    {
      T result;
      using (var cookie = lifetime.UsingExecuteIfAlive())
      {
        if (!cookie.Success) return Result<T>.Unsuccess;

        result = open();
        if (lifetime.TryOnTermination(() => close(result))) 
          return Result.Create(result);
      }
      
      close(result);
      return Result.Create(result);
    }

    public static bool TryBracket(this Lifetime lifetime, Action open, Action close)
    {
      using (var cookie = lifetime.UsingExecuteIfAlive())
      {
        if (!cookie.Success) return false;

        open();
        if (lifetime.TryOnTermination(close))
          return true;
      }

      close();
      return true;
    }

    public static T Bracket<T>(this Lifetime lifetime, Func<T> open, Action<T> close)
    {
      var result = lifetime.TryBracket(open, close);
      return result.Success ? result.Value : throw new LifetimeCanceledException(lifetime);
    }

    public static void Bracket(this Lifetime lifetime, Action open, Action close)
    {
      if (!lifetime.TryBracket(open, close)) 
        throw new LifetimeCanceledException(lifetime);
    }
    
    public static void OnTermination(this Lifetime lifetime, Action action)
    {
      if (!lifetime.TryOnTermination(action))
        throw new LifetimeCanceledException(lifetime);
    }

    public static void OnTermination(this Lifetime lifetime, IDisposable disposable)
    {
      if (!lifetime.TryOnTermination(disposable))
        throw new LifetimeCanceledException(lifetime);
    }
    
    public static void ThrowIfNotAlive(this Lifetime lifetime)
    {
      if (lifetime.IsNotAlive) 
        throw new LifetimeCanceledException(lifetime);
    }

    public static LifetimeDefinition.ExecuteIfAliveCookie UsingExecuteIfAliveOrThrow(this Lifetime lifetime)
    {
      var cookie = lifetime.UsingExecuteIfAlive();
      if (!cookie.Success) throw new LifetimeCanceledException(lifetime); // no need to dispose if success is false // todo add tests for this case

      return cookie;
    }

    public static void KeepAlive(this Lifetime lifetime, object obj) => lifetime.OnTermination(() => GC.KeepAlive(obj));
    public static bool TryKeepAlive(this Lifetime lifetime, object obj) => lifetime.TryOnTermination(() => GC.KeepAlive(obj));

    public static bool AddOrTerminate(this Lifetime lifetime, Action action)
    {
      if (lifetime.TryOnTermination(action))
        return true;
      
      action();
      return false;
    }

    public static TaskCompletionSource<T> CreateTaskCompletionSource<T>(this Lifetime lifetime, TaskCreationOptions options = TaskCreationOptions.None)
    {
      var tcs = new TaskCompletionSource<T>(options);
      if (lifetime.IsEternal) return tcs;

      if (lifetime.IsAlive)
        return tcs.SynchronizeWith(lifetime.DefineNested());

      tcs.TrySetCanceled(lifetime);
      return tcs;
    }

    public static TaskCompletionSource<T> SynchronizeWith<T>(this TaskCompletionSource<T> tcs, LifetimeDefinition def)
    {
      if (def.Lifetime.AddOrTerminate(() => tcs.TrySetCanceled(def.Lifetime)))
        tcs.Task.ContinueWith(_ => def.Terminate());
      
      return tcs;
    }

    public static void UsingNested(this Lifetime lifetime, Action<Lifetime> action)
    {
      using var def = lifetime.DefineNested();
      action(def.Lifetime);
    }
    
    public static T UsingNested<T>(this Lifetime lifetime, Func<Lifetime, T> action)
    {
      using var def = lifetime.DefineNested();
      return action(def.Lifetime);
    }
    
    public static async Task UsingNestedAsync(this Lifetime lifetime, Func<Lifetime, Task> action)
    {
      using var def = lifetime.DefineNested();
      await action(def.Lifetime);
    }
    
    public static async Task<T> UsingNestedAsync<T>(this Lifetime lifetime, Func<Lifetime, Task<T>> action)
    {
      using var def = lifetime.DefineNested();
      return await action(def.Lifetime);
    }
  }
}