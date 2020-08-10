using System.Threading.Tasks;

namespace Common.Channels
{
    public interface IAsyncSender<in T>
    {
        Task SendAsync(T message);
    }
}