using System;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace Common.Reflection.Emit
{
  public static class IlGeneratorUtil
  {
    public static ILGenerator LoadThis(this ILGenerator il)
    {
      il.Emit(OpCodes.Ldarg_0);
      return il;
    }

    public static ILGenerator LoadArgByIndex(this ILGenerator il, int index)
    {
      switch (index)
      {
        case 0: il.Emit(OpCodes.Ldarg_1); break;
        case 1: il.Emit(OpCodes.Ldarg_2); break;
        case 2: il.Emit(OpCodes.Ldarg_3); break;

        default: il.Emit(OpCodes.Ldarg_S, index); break;
      }

      return il;
    }

    public static ILGenerator LoadArgs(this ILGenerator il, int count)
    {
      for (var i = 0; i < count; i++) il.LoadArgByIndex(i);
      return il;
    }

    public static ILGenerator LoadInstanceField(this ILGenerator il, FieldBuilder fieldBuilder)
    {
      il.LoadThis();
      il.Emit(OpCodes.Ldfld, fieldBuilder);
      return il;
    }

    public static ILGenerator Call(this ILGenerator il, MethodInfo methodInfo)
    {
      il.Emit(OpCodes.Call, methodInfo);
      return il;
    }

    public static ILGenerator CallVirt(this ILGenerator il, MethodInfo methodInfo)
    {
      il.Emit(OpCodes.Callvirt, methodInfo);
      return il;
    }

    public static ILGenerator ConstrainedCallVirt<T>(this ILGenerator il, MethodInfo methodInfo)
    {
      il.Emit(OpCodes.Constrained, typeof(T));
      il.CallVirt(methodInfo);
      return il;
    }

    public static ILGenerator ConstrainedCall<T>(this ILGenerator il, MethodInfo methodInfo)
    {
      il.Emit(OpCodes.Constrained, typeof(T));
      il.Call(methodInfo);
      return il;
    }

    public static LocalVariable StoreToLocal(this ILGenerator il, int index)
    {
      var localVariable = new LocalVariable(il, index);
      localVariable.Store();
      return localVariable;
    }

    public static void Return(this ILGenerator il) => il.Emit(OpCodes.Ret);

    [MustUseReturnValue]
    public static TryScope EmitTry(this ILGenerator il, [InstantHandle] Action<ILGenerator> action)
    {
      var scope = new TryScope(il);
      action(il);
      return scope;
    }
  }
  
  public readonly struct TryScope
  {
    public ILGenerator Il { get; }

    public TryScope(ILGenerator il)
    {
      Il = il;
      il.BeginExceptionBlock();
    }

    public void EmitFinally([InstantHandle] Action<ILGenerator> action)
    {
      Il.BeginFinallyBlock();
      action(Il);
      EndOfScope();
    }

    public void EndOfScope() => Il.EndExceptionBlock();
  }

  public readonly struct LocalVariable
  {
    private readonly ILGenerator myIl;
    private readonly int myIndex;

    public LocalVariable(ILGenerator il, int index)
    {
      myIl = il;
      myIndex = index;
    }

    public void Store()
    {
      switch (myIndex)
      {
        case 0: myIl.Emit(OpCodes.Stloc_0); break;
        case 1: myIl.Emit(OpCodes.Stloc_1); break;
        case 2: myIl.Emit(OpCodes.Stloc_2); break;
        case 3: myIl.Emit(OpCodes.Stloc_3); break;

        default: myIl.Emit(OpCodes.Stloc_S, myIndex); break;
      }
    }
      
    public void Load()
    {
      switch (myIndex)
      {
        case 0: myIl.Emit(OpCodes.Ldloc_0); break;
        case 1: myIl.Emit(OpCodes.Ldloc_1); break;
        case 2: myIl.Emit(OpCodes.Ldloc_2); break;
        case 3: myIl.Emit(OpCodes.Ldloc_3); break;

        default: myIl.Emit(OpCodes.Ldloc_S, myIndex); break;
      }
    }
  }
}