using System;
using System.Collections.Generic;

namespace Common.DataStructures
{
  public readonly struct StringSlice : IEquatable<StringSlice>
  {
    public static Comparer EqualityComparer { get; } = new Comparer();

    private readonly string myString;
    private readonly int myStart;
      
    public int Length { get; }
    public ReadOnlySpan<char> Value => myString.AsSpan(myStart, Length);

    public StringSlice(string s, int start, int length)
    {
      myString = s;
      myStart = start;
      Length = length;
    }
      
    public StringSlice(string s, int start = 0)
    {
      myString = s;
      myStart = start;
      Length = s.Length - start;
    }

    public bool Equals(StringSlice other) => EqualityComparer.Equals(this, other);
      
    public override bool Equals(object? obj) => obj is StringSlice slice && Equals(slice);
    public override int GetHashCode() => EqualityComparer.GetHashCode(this);

    public override string ToString() => Value.ToString();

    public class Comparer : IEqualityComparer<StringSlice>
    {
      public bool Equals(StringSlice x, StringSlice y)
      {
        return x.Value == y.Value || x.Value.SequenceEqual(y.Value);
      }

      public int GetHashCode(StringSlice obj) => string.GetHashCode(obj.Value);
    }
  }
}