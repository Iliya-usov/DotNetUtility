using System;
using JetBrains.Annotations;
using NUnit.Framework;

namespace TestUtils
{
  public static class TestUtil
  {
    public static T ShouldBe<T>(this T actual, T expected, string? message = null)
    {
      Assert.AreEqual(expected, actual, message);
      return actual;
    }

    public static T ShouldNotBe<T>(this T actual, T expected, string? message = null)
    {
      Assert.AreNotEqual(expected, actual, message);
      return actual;
    }

    [ContractAnnotation("actual:false => halt")]
    public static bool ShouldBeTrue(this bool actual, string? message = null)
    {
      actual.ShouldBe(true, message);
      return actual;
    }

    [ContractAnnotation("actual:true => halt")]
    public static bool ShouldBeFalse(this bool actual, string? message = null)
    {
      actual.ShouldBe(false, message);
      return actual;
    }

    [ContractAnnotation("actual:notnull => halt")]
    public static T ShouldBeNull<T>(this T actual, string? message = null)
    {
      ((object?)actual).ShouldBe(null, message);
      return actual;
    }

    [ContractAnnotation("actual:null => halt;")]
    public static T ShouldNotBeNull<T>(this T actual, string? message = null)
    {
      ((object?)actual).ShouldNotBe(null, message);
      return actual;
    }

    public static T ShouldBeLessThan<T>(this T actual, T value, string? message = null) where T : IComparable
    {
      Assert.Less(actual, value, message);
      return actual;
    }

    public static T ShouldBeGreaterThan<T>(this T actual, T value, string? message = null) where T : IComparable
    {
      Assert.Greater(actual, value, message);
      return actual;
    }

    public static T ShouldBeLessOrEqualThan<T>(this T actual, T value, string? message = null) where T: IComparable
    {
      Assert.LessOrEqual(actual, value, message);
      return actual;
    }

    public static T ShouldBeGreaterOrEqualThan<T>(this T actual, T value, string? message = null) where T : IComparable
    {
      Assert.GreaterOrEqual(actual, value, message);
      return actual;
    }

    public static void ShouldBeIs<T>(this object value, string? message = null) => Assert.IsInstanceOf<T>(value);
    public static void ShouldNotBeIs<T>(this object value, string? message = null) => Assert.IsNotInstanceOf<T>(value);
  }
}