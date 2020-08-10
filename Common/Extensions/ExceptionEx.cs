using System;
using System.Threading;

namespace Common.Extensions
{
  public static class ExceptionEx
  {
    public static bool IsOperationCancelled(this Exception e)
    {
      return e switch
      {
        OperationCanceledException _ => true,
        AggregateException aggregateException => aggregateException.IsOperationCancelled(),
        _ => e.InnerException is {} inner && inner.IsOperationCancelled()
      };
    }   
    
    public static bool IsOperationCancelled(this Exception e, CancellationToken token)
    {
      return e switch
      {
        OperationCanceledException oce => oce.CancellationToken == token,
        AggregateException aggregateException => aggregateException.IsOperationCancelled(token),
        _ => e.InnerException is {} inner && inner.IsOperationCancelled(token)
      };
    }

    public static bool IsOperationCancelled(this AggregateException aggregateException)
    {
      // ReSharper disable once ForCanBeConvertedToForeach
      // ReSharper disable once LoopCanBeConvertedToQuery
      // Please don't convert to foreach or linq, because InnerExceptions doesn't have
      // struct enumerator and it will allocate a new instance in the heap
      for (var index = 0; index < aggregateException.InnerExceptions.Count; index++)
      {
        if (!aggregateException.InnerExceptions[index].IsOperationCancelled())
          return false;
      }

      return true;
    }
    
    public static bool IsOperationCancelled(this AggregateException aggregateException, CancellationToken token)
    {
      // ReSharper disable once ForCanBeConvertedToForeach
      // ReSharper disable once LoopCanBeConvertedToQuery
      // Please don't convert to foreach or linq, because InnerExceptions doesn't have
      // struct enumerator and it will allocate a new instance in the heap
      for (var index = 0; index < aggregateException.InnerExceptions.Count; index++)
      {
        if (!aggregateException.InnerExceptions[index].IsOperationCancelled(token))
          return false;
      }

      return true;
    }

  }
}