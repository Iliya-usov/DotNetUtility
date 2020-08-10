using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Lifetimes;
using Common.Logging;

namespace Common.Channels
{
    // todo tests
    public class Actor<T>
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<Actor<T>>();
    
        private readonly AsyncChannel<T> myChannel;

        public int Count => myChannel.Count;

        public Actor(Lifetime lifetime, string id, Action<T> processor, TaskScheduler? scheduler = null) :
            this(lifetime, id, t =>
            {
                processor(t);
                return Task.CompletedTask;
            }, scheduler)
        {
        }

        public Actor(Lifetime lifetime, string id, Func<T, Task> processor, TaskScheduler? scheduler = null)
        {
            myChannel = new AsyncChannel<T>(lifetime, $"{id}_{nameof(AsyncChannel<T>)}");
            Task.Factory.StartNew<Task>(async () =>
            {
                while (lifetime.IsAlive)
                {
                    try
                    {
                        var value = await myChannel.ReceiveAsync();
                        await processor(value);
                    }
                    catch (Exception e) when (e.IsOperationCancelled())
                    {
                        // ignore
                    }
                    catch (Exception e)
                    {
                        ourLogger.Error(e, $"Error in {id} actor");
                    }
                }
                
                // todo assert message count
            }, CancellationToken.None, TaskCreationOptions.None, scheduler ?? TaskScheduler.Default).NoAwait();
        }

        public Task SendAsync(T message) => myChannel.SendAsync(message);
    }
}