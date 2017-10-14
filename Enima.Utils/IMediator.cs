using System;
using System.Threading.Tasks;

namespace Enima.Utils {
    public interface IMediator<T> {
        void Send<M>(T topic, M message);
        void SendAll<M>(T topic, params M[] messages);
        void Post<M>(T topic, M message);
        void PostAll<M>(T topic, params M[] messages);
        void Post<M>(T topic, TaskScheduler scheduler, M message);
        void PostAll<M>(T topic, TaskScheduler scheduler, params M[] messages);
        void AddSubscriber(ISubscriber<T> subscriber);
        void RemoveSubscriber(ISubscriber<T> subscriber);
        bool AddHandler(T topic, Delegate handler);
        void RemoveHandler(T topic, Delegate handler);
    }
}
