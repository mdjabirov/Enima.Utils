using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Enima.Utils {
    public class Mediator<T> : IMediator<T> {
                
        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible parameter type signature with the message passed
        /// Compatible handler delegate types are Action<typeparamref name="T1"/> and Action<object[]>
        /// </summary>
        public void Send<T1>(T topic, T1 message) {
            SendInternal(topic, del => {
                Action<T1> action = del as Action<T1>;
                if (action != null) {
                    action(message);
                }
                else {
                    Action<object[]> action2 = (Action<object[]>) del;
                    action2(new object[] { message });
                }
            } );
        }

        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible params type signature with the args array passed
        /// Compatible handler delegate types are Action<typeparamref name="T1"[]/> and Action<object[]>
        /// </summary>
        public void SendAll<T1>(T topic, params T1[] args) {
            SendInternal(topic, del => {
                Action<T1[]> action = del as Action<T1[]>;
                if (action != null) {
                    action(args);
                }
                else {
                    Action<object[]> action2 = (Action<object[]>) del;
                    object[] messageObjects = new object[args.Length];
                    for (int i = 0; i < args.Length; i++) {
                        messageObjects[i] = args[i];
                    }
                    action2(messageObjects);
                }
            });
        }

        /// <summary>
        /// Call all no-argument subscribers for the topic
        /// </summary>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        public void Send(T topic) {
            SendInternal(topic, del => {
                Action action = (Action) del;
                action();
            });
        }

        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible parameter type signature with the message passed
        /// Compatible handler delegate types are Action<T1, T2> and Action<object[]>
        /// </summary>
        public void Send<T1, T2>(T topic, T1 arg1, T2 arg2) {
            SendInternal(topic, del => {
                Action<T1, T2> action = del as Action<T1, T2>;
                if (action != null) {
                    action(arg1, arg2);
                }
                else {
                    Action<object[]> action2 = (Action<object[]>) del;
                    action2(new object[] { arg1, arg2 });
                }
            });
        }

        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible parameter type signature with the message passed
        /// Compatible handler delegate types are Action<T1, T2, T3> and Action<object[]>
        /// </summary>
        public void Send<T1, T2, T3>(T topic, T1 arg1, T2 arg2, T3 arg3) {
            SendInternal(topic, del => {
                Action<T1, T2, T3> action = del as Action<T1, T2, T3>;
                if (action != null) {
                    action(arg1, arg2, arg3);
                }
                else {
                    Action<object[]> action2 = (Action<object[]>) del;
                    action2(new object[] { arg1, arg2, arg3 });
                }
            });
        }

        /// <summary>
        /// Synchronously calls all topic subscriber handlers having compatible parameter type signature with the message passed
        /// Compatible handler delegate types are Action<T1, T2, T3, T4> and Action<object[]>
        /// </summary>
        public void Send<T1, T2, T3, T4>(T topic, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            SendInternal(topic, del => {
                Action<T1, T2, T3, T4> action = del as Action<T1, T2, T3, T4>;
                if (action != null) {
                    action(arg1, arg2, arg3, arg4);
                }
                else {
                    Action<object[]> action2 = (Action<object[]>) del;
                    action2(new object[] { arg1, arg2, arg3, arg4 });
                }
            });
        }

        /// <summary>
        /// For each subscriber interested in the topic, find the handlers for that topic and call the sendAction passed
        /// </summary>
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
                IList<Delegate> handlers = ((ISubscriber<T>) wr.Target).GetHandlers(topic);
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
