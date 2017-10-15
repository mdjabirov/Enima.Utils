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

        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible parameter type signature with the message passed
        /// </summary>
        /// <typeparam name="M">The type of messsage argument passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="message">The single message argument passed</param>
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

        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible params type signature with the messages array passed
        /// </summary>
        /// <typeparam name="M">The type of messsages in the params array passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="messages">The array of messages passed</param>
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

        /// <summary>
        /// Call all no-argument subscribers for the topic
        /// </summary>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
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
           
        /// <summary>
        /// Asynchronous version of <see cref="Send{M}(T, M)"/>
        /// Starts a task with the TaskScheduler.Default scheduler to call <see cref="Send{M}(T, M)"/>
        /// </summary>
        /// <typeparam name="M">The type of messsage argument passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="message">The single message argument passed</param>
        public Task Post<M>(T topic, M message) {
            Task task = new Task(() => {
                Send(topic, message);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        public Task PostAll<M>(T topic, params M[] messages) {
            Task task = new Task(() => {
                SendAll(topic, messages);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        public Task Post(T topic) {
            Task task = new Task(() => {
                Send(topic);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        public Task[] PostParallel(T topic) {
            // TODO:
            throw new NotImplementedException();
        }

        public Task[] PostParallel<M>(T topic, M message) {
            // TODO:
            throw new NotImplementedException();
        }

        public Task[] PostParallelAll<M>(T topic, params M[] messages) {
            // TODO:
            throw new NotImplementedException();
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
