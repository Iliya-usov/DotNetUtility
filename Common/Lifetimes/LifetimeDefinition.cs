using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Common.DataStructures;
using Common.Logging;

namespace Common.Lifetimes
{
  // todo terminate async
  // todo lock -> mutex coockie
  // todo bit slices
  // todo tests
  public class LifetimeDefinition : IDisposable
  {
    private static readonly ILogger ourLogger  = Logger.GetLogger<LifetimeDefinition>();

    public static IntBitSlice<LifetimeStatus> LifetimeStatusSlice { get; } = new IntBitSlice<LifetimeStatus>();
    public static IntBitSlice<ushort> ExecutionCountSlice { get; } = LifetimeStatusSlice.Next<ushort>();
    
    internal static LifetimeDefinition Terminated { get; } = new LifetimeDefinition(LifetimeStatus.Terminated);

    public static TimeSpan TerminationUnderExecutionTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

    // null, or 
    // single object (handler or LifetimedCancellationTokenSource) or
    // array (first element may be LifetimedCancellationTokenSource) or 
    private object? myToDispose;
    private int myStatus;

    public LifetimeStatus Status => LifetimeStatusSlice.ReadValue(ref myStatus);

    public bool IsAlive => Status == LifetimeStatus.Alive;
    public bool IsNotAlive => !IsAlive;
    
    public Lifetime Lifetime => new Lifetime(this);

    public LifetimeDefinition(Lifetime parent)
    {
      if (parent.IsNotAlive)
      {
        LifetimeStatusSlice.WriteValue(ref myStatus, LifetimeStatus.Terminated);
        return;
      }

      parent.Def?.AttachOrTerminate(this);
    }

    public LifetimeDefinition() { }

    private LifetimeDefinition(LifetimeStatus status)
    {
      LifetimeStatusSlice.WriteValue(ref myStatus, status);
    }

    ~LifetimeDefinition()
    {
      if (Status == LifetimeStatus.Terminated) return;

      ourLogger.Error("Memory leak detected");
      Terminate();
    }

    public CancellationToken ToCancellationToken() => TryGetOrCreateCancellationTokenSource()?.Token ?? new CancellationToken(true);

    private LifetimedCancellationTokenSource? TryGetCancellationTokenSource()
    {
      // ReSharper disable once InconsistentlySynchronizedField
      return myToDispose switch
      {
        LifetimedCancellationTokenSource source => source,
        object?[] array when array[0] is LifetimedCancellationTokenSource source => source,
        _ => null
      };
    }

    private LifetimedCancellationTokenSource? TryGetOrCreateCancellationTokenSource()
    {
      var cachedSource = TryGetCancellationTokenSource();
      if (cachedSource != null) return cachedSource;
      
      lock (this) // todo use mutex instead of lock (this)
      {
        switch (myToDispose)
        {
          case null:
          {
            if (IsNotAlive) return null;
            
            var sourceHolder = new LifetimedCancellationTokenSource();
            myToDispose = sourceHolder;
            return sourceHolder;
          }

          case LifetimedCancellationTokenSource source:
            return source;
          
          case object?[] array when array[0] is LifetimedCancellationTokenSource source: 
            return source;

          case object?[] array:
          {
            if (IsNotAlive) return null;
            
            var source = new LifetimedCancellationTokenSource();
            if (array[^1] == null)
              Array.Copy(array, 0, array, 1, array.Length - 1);
            else
            {
              var newArray = new object?[array.Length + 1]; // todo 2?
              Array.Copy(array, 0, newArray, 1, array.Length);
              myToDispose = newArray;
              array = newArray;
            }
            
            array[0] = source;
            return source;
          }

          case { } obj:
          {
            if (IsNotAlive) return null;

            var source = new LifetimedCancellationTokenSource();
            myToDispose = new {source, obj};
            return source;
          }            
        }
      }
    }

    public ExecuteIfAliveCookie UsingExecuteIfAlive() => new ExecuteIfAliveCookie(Lifetime);

    public bool TryOnTermination(Action action) => TryAddObjectToDispose(action);
    public bool TryOnTermination(IDisposable disposable) => TryAddObjectToDispose(disposable);

    public void Terminate()
    {
      if (Status >= LifetimeStatus.Terminating) return;

      if (ThreadLocalCookie.IsExecutionRunningOnCurrentThread(this))
        throw new InvalidOperationException("Termination under execution is not allowed");

      CancelAllRecursively();

      if (IsExecutionRunning())
      {
        var success = SpinWait.SpinUntil(() =>
        {
          return ExecutionCountSlice.ReadValue(ref myStatus) == 0 || Status >= LifetimeStatus.Terminating;
        }, TerminationUnderExecutionTimeout);

        if (!success)
          ourLogger.Error($"Execution is not ended for timeout: {TerminationUnderExecutionTimeout}");
      }

      if (!CompareAndSet(LifetimeStatus.Cancelling, LifetimeStatus.Terminating))
        return;

      Terminate(GetToDisposeObjectUnderLock(true));
      LifetimeStatusSlice.Update(ref myStatus, LifetimeStatus.Terminated);
      GC.SuppressFinalize(this);
    }

    private bool IsExecutionRunning() => ExecutionCountSlice.ReadValue(ref myStatus) != 0;

    internal void AttachOrTerminate(LifetimeDefinition nested)
    {
      if (!TryAttach(nested))
        nested.Terminate();
    }
    
    internal void AttachOrThrow(LifetimeDefinition nested)
    {
      if (!TryAttach(nested))
        throw new LifetimeCanceledException(Lifetime);
    }

    internal bool TryAttach(LifetimeDefinition nested) => TryAddObjectToDispose(nested);

    private static void Terminate(object? obj)
    {
      if (obj is LifetimedCancellationTokenSource source)
      {
        source.Terminate();
        return;
      }
      TerminateImpl(obj);
    }

    private static void TerminateImpl(object? obj)
    {
      try
      {
        switch (obj)
        {
          case null: return;
          
          case LifetimeDefinition def:
            def.Terminate();
            return;
          
          case IDisposable disposable:
            disposable.Dispose();
            return;
          
          case Action action:
            action();
            return;
          
          case object?[] array:
            var lastEmptyIndex = GetLastEmptyIndex(array);
            var minIndex = 0;
            if (array[0] is LifetimedCancellationTokenSource source)
            {
              source.Terminate();
              minIndex = 1;
            }
            for (var index = lastEmptyIndex - 1; index >= minIndex; index--)
              TerminateImpl(array[index]);
            
            return;
        }
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Failed to terminate object");
      }
    }

    private bool TryAddObjectToDispose(object toDispose)
    {
      if (Status >= LifetimeStatus.Terminating) return false;
      
      lock (this)
      {
        if (Status >= LifetimeStatus.Terminating) return false;

        switch (myToDispose)
        {
          case null:
            myToDispose = toDispose;
            return true;
          case object?[] array:
            var lastEmptyIndex = GetLastEmptyIndex(array);
            if (lastEmptyIndex < array.Length)
            {
              array[lastEmptyIndex] = toDispose;
              return true;
            }
            else if (ClearTerminatedLifetimes(array, out lastEmptyIndex))
            {
              array[lastEmptyIndex] = toDispose;
              return true;
            }
            else
            {
              var newArray = new object?[array.Length * 2]; // todo 2? 
              if (newArray.Length < 10000 || array[0] is LifetimedCancellationTokenSource)
                Array.Copy(array, newArray, array.Length);
              else
              {
                Array.Copy(array, 0, newArray, 1, array.Length);
                newArray[0] = new LifetimedCancellationTokenSource();
                lastEmptyIndex++;
              }
              newArray[lastEmptyIndex] = toDispose;
              myToDispose = newArray;
              return true;
            }
          case { } obj:
            myToDispose = new[] {obj, toDispose};
            return true;
        }
      }
    }

    private void CancelAllRecursively()
    {
      if (IsNotAlive) return;

      if (!CompareAndSet(LifetimeStatus.Alive, LifetimeStatus.Cancelling))
        return;

      // ReSharper disable once InconsistentlySynchronizedField
      switch (myToDispose)
      {
        case object?[] array:
          foreach (var obj in array)
          {
            if (obj is LifetimeDefinition def)
              def.CancelAllRecursively();
            else if (obj == null) break;
          }

          break;
        case LifetimeDefinition def:
          def.CancelAllRecursively();
          break;
      }

      TryGetCancellationTokenSource()?.Terminate();
    }

    private static bool ClearTerminatedLifetimes(object?[] array, out int emptyIndex)
    {
      var invalidIndex = array.Length;
      emptyIndex = invalidIndex;
      for (var i = 0; i < array.Length; i++)
      {
        ref var value = ref array[i];
        if (value == null) break;

        if (value is LifetimeDefinition def && def.Status >= LifetimeStatus.Terminating)
        {
          if (emptyIndex == invalidIndex)
            emptyIndex = i;

          value = null;
        }
        else if (emptyIndex != invalidIndex)
        {
          array[emptyIndex++] = value;
          value = null;
        }
      }

      return emptyIndex != invalidIndex;
    }

    private static int GetLastEmptyIndex(object?[] array)
    {
      if (array[0] == null) return 0;
      if (array[^1] != null) return array.Length;
      if (array[^2] != null) return array.Length - 1; // array has a minimum of 2 elements

      var left = 1;
      var right = array.Length - 2;

      while (left < right)
      {
        var mid = left + ((right - left) >> 1);
        if (array[mid] == null)
        {
          if (array[mid - 1] != null) return mid;

          right = mid;
        }
        else
        {
          if (array[mid + 1] == null) return mid + 1;

          left = mid;
        }
      }

      return array.Length;
    }

    private object? GetToDisposeObjectUnderLock(bool reset = false)
    {
      lock (this)
      {
        var toDispose = myToDispose;
        if (!reset) return toDispose;

        myToDispose = toDispose switch
        {
          LifetimedCancellationTokenSource source => source,
          object?[] array when array[0] is LifetimedCancellationTokenSource source => source,
          _ => null
        };
        return toDispose;
      }
    }

    private bool TryIncrementExecutionCount()
    {
      // todo 
      return Status == LifetimeStatus.Alive && ExecutionCountSlice.TryIncrement(ref myStatus, i => CheckForInterrupt(i), out _);
    }
    
    private static bool CheckForInterrupt(int status) => LifetimeStatusSlice.ReadValue(ref status) != LifetimeStatus.Alive;

    private void DecrementExecutionCount()
    {
      ExecutionCountSlice.Decrement(ref myStatus);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CompareAndSet(LifetimeStatus oldStatus, LifetimeStatus newStatus)
    {
      return LifetimeStatusSlice.TryUpdate(ref myStatus, newStatus, oldStatus);
    }

    private readonly struct ThreadLocalCookie : IDisposable
    {
      [ThreadStatic] private static WeakReference<Dictionary<LifetimeDefinition, int>>? ourMap;

      public bool IsEmpty => myDef == null;

      private readonly LifetimeDefinition myDef;
      private readonly Dictionary<LifetimeDefinition, int> myMap;
      
      public int ExecutionCount
      {
        get => myMap[myDef];
        private set => myMap[myDef] = value;
      }

      public static bool IsExecutionRunningOnCurrentThread(LifetimeDefinition def)
      {
        if (ourMap == null) return false;
        return def.IsExecutionRunning() &&
               ourMap.TryGetTarget(out var map) &&
               map.TryGetValue(def, out var executionCount) &&
               executionCount > 0;
      }

      public ThreadLocalCookie(LifetimeDefinition def)
      {
        Dictionary<LifetimeDefinition, int>? map;
        if (ourMap == null)
        {
          map = new Dictionary<LifetimeDefinition, int>();
          ourMap = new WeakReference<Dictionary<LifetimeDefinition, int>>(map);
        }
        else if (!ourMap.TryGetTarget(out map))
        {
          map = new Dictionary<LifetimeDefinition, int>();
          ourMap.SetTarget(map);
        }

        if (map.TryGetValue(def, out var executionCount))
          map[def] = executionCount + 1;
        else
          map[def] = 1;

        myDef = def;
        myMap = map;
      }

      public void Dispose()
      {
        if (IsEmpty) return;

        var executionCount = ExecutionCount - 1;
        if (executionCount == 0) myMap.Remove(myDef);
        else ExecutionCount = executionCount;
      }
    }
    
    public void Dispose() => Terminate();

    public readonly struct ExecuteIfAliveCookie : IDisposable
    {
      private readonly LifetimeDefinition? myDef;
      private readonly ThreadLocalCookie myThreadLocalCookie;

      public bool Success { get; }

      internal ExecuteIfAliveCookie(Lifetime lifetime)
      {
        myDef = lifetime.Def;

        Success = myDef?.TryIncrementExecutionCount() ?? true;
        myThreadLocalCookie = Success && myDef != null ? new ThreadLocalCookie(myDef) : default;
      }

      public void Dispose()
      {
        if (!Success) return;

        myDef?.DecrementExecutionCount();
        myThreadLocalCookie.Dispose();
      }
    }

    private class LifetimedCancellationTokenSource : CancellationTokenSource
    {
      public void Terminate()
      {
        try
        {
          Cancel();
        }
        catch (Exception e)
        {
          ourLogger.Error(e, "Failed to cancel token source");
        }
      }
    }
  }
}