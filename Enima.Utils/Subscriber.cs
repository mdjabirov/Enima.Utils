using System;
using System.Collections.Generic;

namespace Enima.Utils {
    public class Subscriber<T> : ISubscriber<T> {
        public Subscriber(IMediator<T> mediator) {
            mediator.AddSubscriber(this);
        }

        public IList<Delegate> GetHandlers(T topic) {
            _handlersByTopic.TryGetValue(topic, out List<Delegate> handlers);
            return handlers;
        }

        public bool AddHandler(T topic, Delegate handler) {
            // statics are not supported, need a target
            if (handler == null || handler.Target == null) {
                return false;
            }
            if (!ReferenceEquals(handler.Target, this)) {
                return false;
            }
            if (!_handlersByTopic.TryGetValue(topic, out List<Delegate> handlers)) {
                handlers = new List<Delegate>();
                _handlersByTopic.Add(topic, handlers);
            }
            // don't allow double subscription
            if (handlers.Find(h => h.Equals(handler)) != null) {
                return false;
            }
            handlers.Add(handler);
            return true;
        }
        
        public void RemoveHandler(T topic, Delegate handler) {
            // statics are not supported, need a target
            if (handler == null || handler.Target == null) {
                return;
            }
            if (!_handlersByTopic.TryGetValue(topic, out List<Delegate> handlers)) {
                return;
            }
            handlers.RemoveAll(h => h.Equals(handler));
            if (handlers.Count == 0) {
                _handlersByTopic.Remove(topic);
            }
        }

        private readonly ListDictionary<T, Delegate> _handlersByTopic = new ListDictionary<T, Delegate>();
    }
}
