namespace Enima.Utils {
    public interface IMediator<T> {
        void Send(T topic);
        void Send<T1>(T topic, T1 arg);
        void Send<T1, T2>(T topic, T1 arg1, T2 arg2);
        void Send<T1, T2, T3>(T topic, T1 arg1, T2 arg2, T3 arg3);
        void Send<T1, T2, T3, T4>(T topic, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        void SendAll<T1>(T topic, params T1[] args);
        
        void AddSubscriber(ISubscriber<T> subscriber);
        void RemoveSubscriber(ISubscriber<T> subscriber);
    }
}
