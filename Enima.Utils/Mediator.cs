using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Enima.Utils {
    public class Mediator<T> : IMediator<T> {
                
        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible parameter type signature with the message passed
        /// </summary>
        /// <typeparam name="M">The type of messsage argument passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="message">The single message argument passed</param>
        public void Send<M>(T topic, M message) {
            SendInternal(topic, del => {
                Action<M> action = del as Action<M>;
                if (action != null) {
                    action(message);
                }
            } );
        }

        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible params type signature with the messages array passed
        /// </summary>
        /// <typeparam name="M">The type of messsages in the params array passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="messages">The array of messages passed</param>
        public void SendAll<M>(T topic, params M[] messages) {
            SendInternal(topic, del => {
                Action<M[]> action = del as Action<M[]>;
                if (action != null) {
                    action(messages);
                }
            });
        }

        /// <summary>
        /// Call all no-argument subscribers for the topic
        /// </summary>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        public void Send(T topic) {
            SendInternal(topic, del => {
                Action action = del as Action;
                if (action != null) {
                    action();
                }
            });
        }

        public void Send<M1, M2>(T topic, M1 message1, M2 message2) {
            SendInternal(topic, del => {
                Action<M1, M2> action = del as Action<M1, M2>;
                if (action != null) {
                    action(message1, message2);
                }
            });
        }

        public void Send<M1, M2, M3>(T topic, M1 message1, M2 message2, M3 message3) {
            SendInternal(topic, del => {
                Action<M1, M2, M3> action = del as Action<M1, M2, M3>;
                if (action != null) {
                    action(message1, message2, message3);
                }
            });
        }

        public void Send<M1, M2, M3, M4>(T topic, M1 message1, M2 message2, M3 message3, M4 message4) {
            SendInternal(topic, del => {
                Action<M1, M2, M3, M4> action = del as Action<M1, M2, M3, M4>;
                if (action != null) {
                    action(message1, message2, message3, message4);
                }
            });
        }

        // helper method to avoid code repeition in Sends
        protected void SendInternal(T topic, Action<Delegate> sendAction) {
            List<WeakReference> subscribers;
            if (!_subscribersByTopic.TryGetValue(topic, out subscribers)) {
                return;
            }
            for (int i = 0; i < subscribers.Count; i++) {
                WeakReference wr = subscribers[i];
                if (wr == null || !wr.IsAlive) {
                    continue;
                }
                IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                if (handlers == null) {
                    return;
                }
                for (int j = 0; j < handlers.Count; j++) {
                    sendAction(handlers[j]);
                }
            }
        }

        /// <summary>
        /// Registers a subscriber with the mediator. 
        /// The subscriber's public and non-public instance methods with <see cref="IHandlerAttribute{T}"/> attribute are discovered. 
        /// Delegates with types derived from the method signatures are constructed pointing to the methods.
        /// The delegates are then linked to the topic in <see cref="IHandlerAttribute{T}"/> so that a send or post on that topic invokes them.
        /// Registering a subscriber does not keep it alive in GC as the mediator keeps week references to the handler delegates
        /// </summary>
        /// <param name="subscriber">The subscriber object being registred</param>
        public void AddSubscriber(ISubscriber<T> subscriber) {
            // BindingFlags.Static not supported yet
            MethodInfo[] methods = subscriber.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo mi in methods) {
                foreach (object attr in mi.GetCustomAttributes(typeof(IHandlerAttribute<T>), true)) {
                    ParameterInfo[] pi = mi.GetParameters();
                    Type delegateType = GetDelegateType(mi, pi);
                    Delegate handler = Delegate.CreateDelegate(delegateType, subscriber, mi.Name);
                    T topic = ((IHandlerAttribute<T>) attr).Topic;
                    AddHandler(topic, subscriber, handler);
                }
            }
        }

        /// <summary>
        /// Unregisters a subscriber from the mediator - handler methods will no longer be called after this.
        /// No need to call this for GC as the mediator keeps week references to the handler delegates.
        /// </summary>
        /// <param name="subscriber">The subscriber object being unregistered</param>
        public void RemoveSubscriber(ISubscriber<T> subscriber) {
            // BindingFlags.Static not supported yet
            MethodInfo[] methods = subscriber.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo mi in methods) {
                foreach (object attr in mi.GetCustomAttributes(typeof(IHandlerAttribute<T>), true)) {
                    ParameterInfo[] pi = mi.GetParameters();
                    Type delegateType = GetDelegateType(mi, pi);
                    Delegate handler = Delegate.CreateDelegate(delegateType, subscriber, mi.Name);
                    T topic = ((IHandlerAttribute<T>) attr).Topic;
                    RemoveHandler(topic, subscriber, handler);
                }
            }
        }

        private bool AddHandler(T topic, ISubscriber<T> subscriber, Delegate handler) {
            List<WeakReference> subscribers;
            if (!_subscribersByTopic.TryGetValue(topic, out subscribers)) {
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
            List<WeakReference> subscribers;
            if (!_subscribersByTopic.TryGetValue(topic, out subscribers)) {
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
                // if method returns void we pass the array of argument types
                // Expression.GetActionType then returns the matching Action<> by taking the typeArgs
                Type[] paramTypes = new Type[pi.Length];
                for (int i = 0; i < pi.Length; i++) {
                    paramTypes[i] = pi[i].ParameterType;
                }
                return Expression.GetActionType(paramTypes);
            }
            else {
                // if the method has non-void return, we need a Func<>
                // Exppresion.GetFuncType expects the return type to be the last entry in typeArgs
                Type[] paramAndRetTypes = new Type[pi.Length + 1];
                int i = 0;
                for (; i < pi.Length; i++) {
                    paramAndRetTypes[i] = pi[i].ParameterType;
                }
                paramAndRetTypes[i] = mi.ReturnType;
                return Expression.GetFuncType(paramAndRetTypes);
            }
        }

        protected readonly ListDictionary<T, WeakReference> _subscribersByTopic = new ListDictionary<T, WeakReference>();
    }
}
