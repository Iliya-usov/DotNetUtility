using System.Threading;
using System.Threading.Tasks;
using Common.Extensions;
using NUnit.Framework;
using TestUtils;

namespace CommonTests.Tasks
{
  public class TaskExTests
  {
    [Test]
    public void GetCancellationTokenTest()
    {
      var source = new CancellationTokenSource();
      
      {
        var task = Task.Run(() => { }, source.Token);
        var cancellationToken = task.GetCancellationToken();
        cancellationToken.ShouldBe(source.Token);
      }

      source.Cancel();
      
      {
        var task = Task.Run(() => { }, source.Token);
        var cancellationToken = task.GetCancellationToken();
        cancellationToken.ShouldBe(source.Token);
      }
    }
  }
}