using System;
using System.Reflection;
using Common.Lifetimes;

namespace DI
{
  public interface IComponentContainer
  {
    public object Resolve(Type type);
    public object? TryResolve(Type type);

    IContainerBuilder CreateNested(Lifetime lifetime);
  }
  
  public class ComponentContainer
  {
  }

  public interface IContainerBuilder
  {
    IContainerBuilder RegisterAssembly(Assembly assembly);
    IContainerBuilder RegisterAssemblies(params Assembly[] assemblies);

    IContainerBuilder RegisterTypes(Type[] types);
    IContainerBuilder RegisterInstance(object obj, Type asType);

    IComponentContainer Build();
  }
}