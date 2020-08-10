using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Common.Reflection
{
  public static class MethodInfoUtil
  {
    public static Delegate CompileInstanceDelegate(this MethodInfo method, object instance) => CompileDelegate(method, instance);
    public static Delegate CompileStaticDelegate(this MethodInfo method) => CompileDelegate(method, null);

    private static Delegate CompileDelegate(MethodInfo method, object? instance)
    {
      var parameters = method.GetParameters()
        .Select(p => Expression.Parameter(p.ParameterType, p.Name))
        .ToArray();

      var instanceExpression = instance != null ? Expression.Constant(instance) : null; 
      // ReSharper disable once CoVariantArrayConversion
      var call = Expression.Call(instanceExpression, method, parameters);
      return Expression.Lambda(call, parameters).Compile();
    }
  }
}