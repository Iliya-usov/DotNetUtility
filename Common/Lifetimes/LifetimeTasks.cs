using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Lifetimes
{
  public readonly partial struct Lifetime
  {
    public static AsyncLocal<Lifetime> AsyncLocal { get; } = new AsyncLocal<Lifetime>();

    public Task<T> Start<T>(Func<T> action, TaskScheduler? scheduler = null, TaskCreationOptions options = TaskCreationOptions.None)
    {
      using (UsingAsyncExecutionCookie())
        return Task.Factory.StartNew(action, this, options, scheduler ?? TaskScheduler.Default);
    }
    
    public Task Start(Action action, TaskScheduler? scheduler = null, TaskCreationOptions options = TaskCreationOptions.None)
    {
      using (UsingAsyncExecutionCookie())
        return Task.Factory.StartNew(action, this, options, scheduler ?? TaskScheduler.Default);
    }
    
    public Task<T> StartAsync<T>(Func<Task<T>> action, TaskScheduler? scheduler = null, TaskCreationOptions options = TaskCreationOptions.None)
    {
      using (UsingAsyncExecutionCookie())
        return Task.Factory.StartNew(action, this, options, scheduler ?? TaskScheduler.Default).Unwrap();
    }
    
    public Task StartAsync(Func<Task> action, TaskScheduler? scheduler = null, TaskCreationOptions options = TaskCreationOptions.None)
    {
      using (UsingAsyncExecutionCookie())
        return Task.Factory.StartNew(action, this, options, scheduler ?? TaskScheduler.Default).Unwrap();
    }
    
    private AsyncExecutionCookie UsingAsyncExecutionCookie() => new AsyncExecutionCookie(this);
    
    private readonly struct AsyncExecutionCookie : IDisposable
    {
      private readonly Lifetime myOld;

      public AsyncExecutionCookie(Lifetime lifetime)
      {
        myOld = AsyncLocal.Value;
        AsyncLocal.Value = lifetime;
      }
      public void Dispose() => AsyncLocal.Value = myOld;
    }
  }
}