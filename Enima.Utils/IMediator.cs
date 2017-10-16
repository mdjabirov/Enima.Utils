namespace Enima.Utils {
    public interface IMediator<T> {
        void Send(T topic);
        void Send<M>(T topic, M message);
        void Send<M1, M2>(T topic, M1 message1, M2 message2);
        void Send<M1, M2, M3>(T topic, M1 message1, M2 message2, M3 message3);
        void Send<M1, M2, M3, M4>(T topic, M1 message1, M2 message2, M3 message3, M4 message4);
        void SendAll<M>(T topic, params M[] messages);
        
        void AddSubscriber(ISubscriber<T> subscriber);
        void RemoveSubscriber(ISubscriber<T> subscriber);
    }
}
