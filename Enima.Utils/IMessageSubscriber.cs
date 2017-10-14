using System;
using System.Collections.Generic;

namespace Enima.Utils {
    public interface IMessageSubscriber<T> {
        IList<Delegate> GetHandlers(T topic);
        bool AddHandler(T topic, Delegate handler);
        void RemoveHandler(T topic, Delegate handler);
    }
}
