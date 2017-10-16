#if NET40
using System.Threading.Tasks;

namespace Enima.Utils {
    public interface IMediatorAsync<T> : IMediator<T> {
        Task Post<M>(T topic, M message);
        Task PostAll<M>(T topic, params M[] messages);
        Task Post(T topic);

        Task[] PostParallel(T topic);
        Task[] PostParallel<M>(T topic, M message);
        Task[] PostParallelAll<M>(T topic, params M[] messages);
    }
}
#endif
