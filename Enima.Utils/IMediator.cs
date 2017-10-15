using System;
using System.Threading.Tasks;

namespace Enima.Utils {
    public interface IMediator<T> {
        void Send(T topic);
        void Post(T topic);
        void Post(T topic, TaskScheduler scheduler);
        void Send<M>(T topic, M message);
        void SendAll<M>(T topic, params M[] messages);
        void Post<M>(T topic, M message);
        void PostAll<M>(T topic, params M[] messages);
        void Post<M>(T topic, TaskScheduler scheduler, M message);
        void PostAll<M>(T topic, TaskScheduler scheduler, params M[] messages);
        void AddSubscriber(ISubscriber<T> subscriber);
        void RemoveSubscriber(ISubscriber<T> subscriber);
    }
}
