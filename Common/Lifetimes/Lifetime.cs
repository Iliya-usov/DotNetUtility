using System;
using System.Threading;

namespace Common.Lifetimes
{
  public readonly partial struct Lifetime : IEquatable<Lifetime>
  {
    internal LifetimeDefinition? Def { get; }

    public LifetimeStatus Status => Def?.Status ?? LifetimeStatus.Alive;

    public bool IsEternal => Def == null;
    public bool IsNotEternal => !IsEternal;

    public bool IsAlive => Def?.IsAlive ?? true;
    public bool IsNotAlive => !IsAlive;

    public Lifetime(LifetimeDefinition def)
    {
      Def = def;
    }
    
    public LifetimeDefinition.ExecuteIfAliveCookie UsingExecuteIfAlive() => new LifetimeDefinition.ExecuteIfAliveCookie(this);
    
    public bool TryOnTermination(Action action) => Def?.TryOnTermination(action) ?? true;
    public bool TryOnTermination(IDisposable disposable) => Def?.TryOnTermination(disposable) ?? true;

    public CancellationToken ToCancellationToken() => Def?.ToCancellationToken() ?? CancellationToken.None;
    public static implicit operator CancellationToken(Lifetime lifetime) => lifetime.ToCancellationToken();

    public bool Equals(Lifetime other) => ReferenceEquals(Def, other.Def);
    public override bool Equals(object? obj) => obj is Lifetime other && Equals(other);
    public override int GetHashCode() => Def?.GetHashCode() ?? 0;
  }
}