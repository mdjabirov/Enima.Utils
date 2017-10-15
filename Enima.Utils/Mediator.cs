using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace Enima.Utils {
    public class Mediator<T> : IMediator<T> {
        public Mediator() {
            _defaultScheduler = TaskScheduler.Default;
        }
        
        public void Send<M>(T topic, M message) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                return;
            }
            foreach (WeakReference wr in subscribers) {
                if (wr == null || !wr.IsAlive) {
                    continue;
                }
                IList<Delegate> handlers = ((ISubscriber<T>) wr.Target).GetHandlers(topic);
                if (handlers == null) {
                    return;
                }
                foreach (Delegate handler in handlers) {
                    dynamic d = handler;
                    d(message);
                }
            }
        }

        public void SendAll<M>(T topic, params M[] messages) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                return;
            }
            foreach (WeakReference wr in subscribers) {
                if (wr == null || !wr.IsAlive) {
                    continue;
                }
                IList<Delegate> handlers = ((ISubscriber<T>) wr.Target).GetHandlers(topic);
                if (handlers == null) {
                    return;
                }
                foreach (Delegate handler in handlers) {
                    dynamic d = handler;
                    d(messages);
                }
            }
        }

        public void Send(T topic) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                return;
            }
            foreach (WeakReference wr in subscribers) {
                if (wr == null || !wr.IsAlive) {
                    continue;
                }
                IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                if (handlers == null) {
                    return;
                }
                foreach (Delegate handler in handlers) {
                    dynamic d = handler;
                    d();
                }
            }
        }

        public void Post<M>(T topic, M message) {
            Post(topic, _defaultScheduler, message);
        }

        public void Post<M>(T topic, TaskScheduler scheduler, M message) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                return;
            }
            foreach (WeakReference wr in subscribers) {
                if (wr == null || !wr.IsAlive) {
                    continue;
                }
                IList<Delegate> handlers = ((ISubscriber<T>) wr.Target).GetHandlers(topic);
                if (handlers == null) {
                    return;
                }
                foreach (Delegate handler in handlers) {
                    dynamic d = handler;
                    Task task = new Task(() => d(message));
                    task.Start(scheduler);
                }
            }
        }

        public void PostAll<M>(T topic, params M[] messages) {
            PostAll(topic, _defaultScheduler, messages);
        }

        public void PostAll<M>(T topic, TaskScheduler scheduler, params M[] messages) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                return;
            }
            foreach (WeakReference wr in subscribers) {
                if (wr == null || !wr.IsAlive) {
                    continue;
                }
                IList<Delegate> handlers = ((ISubscriber<T>) wr.Target).GetHandlers(topic);
                if (handlers == null) {
                    return;
                }
                foreach (Delegate handler in handlers) {
                    dynamic d = handler;
                    Task task = new Task(() => d(messages));
                    task.Start(scheduler);
                }
            }
        }

        public void Post(T topic) {
            Post(topic, _defaultScheduler);
        }

        public void Post(T topic, TaskScheduler scheduler) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                return;
            }
            foreach (WeakReference wr in subscribers) {
                if (wr == null || !wr.IsAlive) {
                    continue;
                }
                IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                if (handlers == null) {
                    return;
                }
                foreach (Delegate handler in handlers) {
                    dynamic d = handler;
                    Task task = new Task(() => d());
                    task.Start(scheduler);
                }
            }
        }

        public void AddSubscriber(ISubscriber<T> subscriber) {
            // BindingFlags.Static not supported yet
            MethodInfo[] methods = subscriber.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo mi in methods) {
                foreach (object attr in mi.GetCustomAttributes(typeof(IHandlerAttribute<T>), true)) {
                    var ha = (IHandlerAttribute<T>) attr;
                    ParameterInfo[] pi = mi.GetParameters();
                    Type delegateType = GetDelegateType(mi, pi);
                    Delegate handler = Delegate.CreateDelegate(delegateType, subscriber, mi.Name);
                    AddHandler(ha.Topic, subscriber, handler);
                }
            }
        }

        public void RemoveSubscriber(ISubscriber<T> subscriber) {
            // BindingFlags.Static not supported yet
            MethodInfo[] methods = subscriber.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo mi in methods) {
                foreach (object attr in mi.GetCustomAttributes(typeof(IHandlerAttribute<T>), true)) {
                    var ha = (IHandlerAttribute<T>) attr;
                    ParameterInfo[] pi = mi.GetParameters();
                    Type delegateType = GetDelegateType(mi, pi);
                    Delegate handler = Delegate.CreateDelegate(delegateType, subscriber, mi.Name);
                    RemoveHandler(ha.Topic, subscriber, handler);
                }
            }
        }

        private bool AddHandler(T topic, ISubscriber<T> subscriber, Delegate handler) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                subscribers = new List<WeakReference>();
                _subscribersByTopic.Add(topic, subscribers);
            }
            WeakReference wr = subscribers.Find(w => w != null && ReferenceEquals(w.Target, subscriber));
            if (wr == null) {
                wr = new WeakReference(subscriber);
                subscribers.Add(wr);
            }
            return subscriber.AddHandler(topic, handler);
        }

        private void RemoveHandler(T topic, ISubscriber<T> subscriber, Delegate handler) {
            if (!_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                return;
            }
            WeakReference wr = subscribers.Find(w => w != null && ReferenceEquals(w.Target, subscriber));
            if (wr == null) {
                return;
            }
            subscriber.RemoveHandler(topic, handler);
            IList<Delegate> remainingHandlers = subscriber.GetHandlers(topic);
            if (remainingHandlers == null || remainingHandlers.Count == 0) {
                subscribers.Remove(wr);
                if (subscribers.Count == 0) {
                    _subscribersByTopic.Remove(topic);
                }
            }
        }

        private static Type GetDelegateType(MethodInfo mi, ParameterInfo[] pi) {
            if (mi.ReturnType == typeof(void)) {
                Type[] paramTypes = new Type[pi.Length];
                for (int i = 0; i < pi.Length; i++) {
                    paramTypes[i] = pi[i].ParameterType;
                }
                return Expression.GetActionType(paramTypes);
            }
            else {
                Type[] paramAndRetTypes = new Type[pi.Length + 1];
                int i = 0;
                for (; i < pi.Length; i++) {
                    paramAndRetTypes[i] = pi[i].ParameterType;
                }
                paramAndRetTypes[i] = mi.ReturnType;
                return Expression.GetFuncType(paramAndRetTypes);
            }
        }
        
        private TaskScheduler _defaultScheduler;
        private ListDictionary<T, WeakReference> _subscribersByTopic = new ListDictionary<T, WeakReference>();
    }
}
