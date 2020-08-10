using System;
using System.Diagnostics.CodeAnalysis;

namespace Common.Extensions
{
  public static class Extensions
  {
    public static T NotNull<T>([MaybeNull] this T? value, string? message = null)
    {
      return value == null ? throw new NullReferenceException(message ?? nameof(value)) : value;
    }
  }
}