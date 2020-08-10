using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Common.Collections;
using Common.FileSystem;
using Common.Lifetimes;
using Common.Reflection.Emit;

namespace Common.NativeInterop
{
  public static class NativeDllLoader
  {
    public static INativeDll Load(Lifetime lifetime, FileSystemPath path, DllImportSearchPath? searchPath = null)
    {
      // todo check for several loads for equals assemblies
      var dllPtr = lifetime.Bracket(() => NativeLibrary.Load(path.FullName, typeof(NativeLibrary).Assembly, searchPath), NativeLibrary.Free);
      return new NativeDll(lifetime, dllPtr, path.Name);
    }
    
    private class NativeDll : INativeDll
    {
      private const string LifetimeFieldName = "myLifetime";
      private const char Separator = '_';

      private readonly IntPtr myPtr;

      private static readonly Lazy<MethodInfo> ourLazyUsingExecuteIfAliveOrThrowMethodInfo = new Lazy<MethodInfo>(() => typeof(Lifetime).GetMethod(nameof(LifetimeEx.UsingExecuteIfAliveOrThrow))!);
      private static readonly Lazy<MethodInfo> ourLazyDisposeMethodInfo = new Lazy<MethodInfo>(() => typeof(LifetimeDefinition.ExecuteIfAliveCookie).GetMethod(nameof(LifetimeDefinition.ExecuteIfAliveCookie.Dispose))!);

      private readonly ModuleBuilder myModule;

      private readonly SynchronizedDictionary<Type, object> myCache = new SynchronizedDictionary<Type, object>();
      
      public Lifetime Lifetime { get; }

      public NativeDll(in Lifetime lifetime, in IntPtr dllPtr, string id)
      {
        Lifetime = lifetime;
        myPtr = dllPtr;

        // todo check concurrent import
        var assemblyName = new AssemblyName($"NativeDllAssembly_{id}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        myModule = assembly.DefineDynamicModule("NativeModule");
      }

      public T ImportMethod<T>(string name) where T : Delegate => (T) ImportMethod(name, typeof(T));

      public Delegate ImportMethod(string name, Type delegateType)
      {
        using var _ = Lifetime.UsingExecuteIfAliveOrThrow();

        var intPtr = NativeLibrary.GetExport(myPtr, name);
        return Marshal.GetDelegateForFunctionPointer(intPtr, delegateType);
      }

      public T Import<T>() where T : class
      {
        return (T) myCache.GetOrAdd(typeof(T), DoImport);
      }
      
      private object DoImport(Type type)
      {
        if (!type.IsInterface)
          throw new ArgumentException($"{type} is not interface");
        
        var typeBuilder = myModule.DefineType($"{type.Name}Impl");
        typeBuilder.AddInterfaceImplementation(type);
        
        var lifetimeFieldBuilder = typeBuilder.DefineField(LifetimeFieldName, typeof(Lifetime), FieldAttributes.Private);
        
        var methodInfos = type.GetMethods();
        for (var i = 0; i < methodInfos.Length; i++)
        {
          var methodInfo = methodInfos[i];
          
          var parameters = methodInfo.GetParameters();
          var delegateType = GetDelegateType(methodInfo); // todo generate new with customs
        
          var importName = methodInfo.GetCustomAttribute<NativeImportAttribute>(false)?.Name ?? methodInfo.Name;
          var fieldBuilder = typeBuilder.DefineField($"{importName}{Separator}{i}", delegateType, FieldAttributes.Private);
          
          var attributes = methodInfo.Attributes & ~MethodAttributes.Abstract;
          var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();
          var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, attributes, methodInfo.ReturnType, parameterTypes);
        
          var localsCount = 0;
          var il = methodBuilder.GetILGenerator();
          var executeIfAliveCookieVar = il.LoadInstanceField(lifetimeFieldBuilder)
            .Call(ourLazyUsingExecuteIfAliveOrThrowMethodInfo.Value)
            .StoreToLocal(localsCount++);
        
          LocalVariable resultVar = default;
          il.EmitTry(_ =>
          {
            resultVar = il.LoadInstanceField(fieldBuilder)
              .LoadArgs(parameters.Length)
              .CallVirt(delegateType.GetMethod("Invoke")!)
              .StoreToLocal(localsCount++);
        
          }).EmitFinally(_ =>
          {
            executeIfAliveCookieVar.Load();
            il.ConstrainedCallVirt<LifetimeDefinition.ExecuteIfAliveCookie>(ourLazyDisposeMethodInfo.Value); //todo maybe just call
          });
        
          resultVar.Load();
          il.Return();
        }
        
        return InitType(typeBuilder);
      }

      private object InitType(TypeBuilder typeBuilder)
      {
        var type = typeBuilder.CreateTypeInfo();
        if (type == null)
          throw new ArgumentException("Cannot create type");

        var instance = Activator.CreateInstance(type);
        if (instance == null)
          throw new ArgumentException($"Cannot create instance for type: {type.FullName}");

        foreach (var fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
          if (fieldInfo.Name == LifetimeFieldName)
          {
            fieldInfo.SetValue(instance, Lifetime);
            continue;
          }

          var index = fieldInfo.Name.LastIndexOf(Separator);
          var name = fieldInfo.Name.Substring(0, index);
          var @delegate = ImportMethod(name, fieldInfo.FieldType);
          fieldInfo.SetValue(instance, @delegate);
        }

        return instance;
      }
      
      // todo rewrite
      private static Type GetDelegateType(MethodInfo methodInfo)
      {
        var parameterTypes = methodInfo.GetParameters().Select(x=>x.ParameterType);
        var delegateTypes = methodInfo.ReturnType != typeof(void)
          ? parameterTypes.Concat(new[] {methodInfo.ReturnType}).ToArray()
          : parameterTypes.ToArray();

        return Expression.GetDelegateType(delegateTypes);
      }
    }
  }
}