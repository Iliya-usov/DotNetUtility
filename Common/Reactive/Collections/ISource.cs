using System;
using System.Collections.Generic;
using Common.Lifetimes;

namespace Common.Reactive.Collections
{
  public interface ISource<out T>
  {
    void Advice(Lifetime lifetime, Action<T> action);
  }

  public static class Lifetimed // todo move
  {
    public static Lifetimed<T> Create<T>(Lifetime lifetime, T value) => new Lifetimed<T>(lifetime, value);
    public static Lifetimed<T> ToLifetimed<T>(this T value, Lifetime lifetime) => Create(lifetime, value);
  }

  public readonly struct Lifetimed<T> : IEquatable<Lifetimed<T>>
  {
    public Lifetime Lifetime { get; }
    public T Value { get; }

    public Lifetimed(Lifetime lifetime, T value)
    {
      Lifetime = lifetime;
      Value = value;
    }

    public bool Equals(Lifetimed<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is Lifetimed<T> other && Equals(other);
    public override int GetHashCode() => Value != null ? EqualityComparer<T>.Default.GetHashCode(Value) : 0;

    public class Comparer : IEqualityComparer<Lifetimed<T>>
    {
      public static Comparer Instanece { get; } = new Comparer();

      public bool Equals(Lifetimed<T> x, Lifetimed<T> y) => x.Equals(y);
      public int GetHashCode(Lifetimed<T> obj) => obj.GetHashCode();
    }
  }
}