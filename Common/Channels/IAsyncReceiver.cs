using System.Threading.Tasks;

namespace Common.Channels
{
    public interface IAsyncReceiver<T>
    {
        Task<T> ReceiveAsync();
    }
}