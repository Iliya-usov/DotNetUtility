using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Common.Delegates;
using Common.Logging;

namespace Common.Reflection
{
  public static class TaskUtil
  {
    private static readonly ILogger ourLogger = Logger.GetLogger($"{nameof(Reflection)}.{nameof(TaskUtil)}");

    private static readonly Lazy<Func<Task, CancellationToken>> ourLazyGetCancellationToken = new Lazy<Func<Task, CancellationToken>>(() =>
    {
      var propertyInfo = typeof(Task).GetProperty("CancellationToken", BindingFlags.Instance | BindingFlags.NonPublic);
      var getter = propertyInfo?.GetMethod;
      if (getter == null)
      {
        ourLogger.Error("Could not get CancellationToken property from task type"); // todo text
        return OurFunc<Task>.None;
      }

      var dynamicMethod = new DynamicMethod("GetCancellationTokenFromTask", typeof(CancellationToken), new []{typeof(Task)}, true);
      
      var il = dynamicMethod.GetILGenerator();
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Call, getter);
      il.Emit(OpCodes.Ret);

      try
      {
        return (Func<Task, CancellationToken>) dynamicMethod.CreateDelegate(typeof(Func<Task, CancellationToken>));
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Failed to create GetCancellationToken il method"); // todo text
        return OurFunc<Task>.None;
      }
    });

    public static CancellationToken GetCancellationToken(Task task)
    {
      return ourLazyGetCancellationToken.Value(task);
    }
  }
}