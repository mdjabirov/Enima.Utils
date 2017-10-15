using System.Threading.Tasks;

namespace Enima.Utils {
    public interface IMediator<T> {
        void Send(T topic);
        void Send<M>(T topic, M message);
        void SendAll<M>(T topic, params M[] messages);

        Task Post<M>(T topic, M message);
        Task PostAll<M>(T topic, params M[] messages);
        Task Post(T topic);

        Task[] PostParallel(T topic);
        Task[] PostParallel<M>(T topic, M message);
        Task[] PostParallelAll<M>(T topic, params M[] messages);

        void AddSubscriber(ISubscriber<T> subscriber);
        void RemoveSubscriber(ISubscriber<T> subscriber);
    }
}
