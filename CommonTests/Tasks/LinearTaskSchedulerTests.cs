using System.Threading;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Lifetimes;
using Common.Tasks;
using NUnit.Framework;
using TestUtils;

namespace CommonTests.Tasks
{
  public class LinearTaskSchedulerTests
  {
    [Test]
    public void SimpleTest()
    {
      var scheduler = new SequentialTaskScheduler(Lifetime.Eternal, nameof(SimpleTest));

      for (var i = 0; i < 1000; i++)
      {
        var state = 0;
      
        Lifetime.Eternal.StartAsync(async () =>
        {
          CheckAndIncrementState(0);
          var thread = Thread.CurrentThread;
          
          await Task.CompletedTask;
          
          Thread.CurrentThread.ShouldBe(thread);
          CheckAndIncrementState(1);

          var nested = Lifetime.Eternal.Start(() => CheckAndIncrementState(2), scheduler);

          nested.Status.ShouldBe(TaskStatus.WaitingToRun);
          await Task.Yield();
          nested.Status.ShouldBe(TaskStatus.RanToCompletion);

          CheckAndIncrementState(3);
        }, scheduler).Wait(); // todo timeout
        
        CheckAndIncrementState(4);

        // ReSharper disable once AccessToModifiedClosure
        void CheckAndIncrementState(int value) => (state++).ShouldBe(value);
      }
    }
  }
}