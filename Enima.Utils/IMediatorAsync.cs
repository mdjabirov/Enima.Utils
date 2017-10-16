#if NET40
using System.Threading.Tasks;

namespace Enima.Utils {
    public interface IMediatorAsync<T> : IMediator<T> {
        Task Post<T1>(T topic, T1 arg);
        Task PostAll<T1>(T topic, params T1[] args);
        Task Post(T topic);

        Task[] PostParallel(T topic);
        Task[] PostParallel<T1>(T topic, T1 arg);
        Task[] PostParallelAll<T1>(T topic, params T1[] args);
    }
}
#endif
