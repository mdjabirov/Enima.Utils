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
                IList<Delegate> handlers = ((ISubscriber<T>) wr.Target).GetHandlers(topic);
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
        /// <returns>The task started</returns>
        public Task Post<M>(T topic, M message) {
            Task task = new Task(() => {
                Send(topic, message);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        /// <summary>
        /// Asynchronous version of <see cref="SendAll{M}(T, M[])"/>
        /// Starts a task with the TaskScheduler.Default scheduler to call <see cref="SendAll{M}(T, M[])"/>
        /// </summary>
        /// <typeparam name="M">The type of messsages in the params array passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="messages">The array of messages passed</param>
        /// <returns>The task started</returns>
        public Task PostAll<M>(T topic, params M[] messages) {
            Task task = new Task(() => {
                SendAll(topic, messages);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        /// <summary>
        /// Asyncrhonous version of <see cref="Send(T)"/>
        /// Starts a task with the TaskScheduler.Default scheduler to call <see cref="Send(T)"/>
        /// </summary>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <returns>The task started</returns>
        public Task Post(T topic) {
            Task task = new Task(() => {
                Send(topic);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        /// <summary>
        /// Parallel version of <see cref="Post{M}(T, M)"/>
        /// Unlike Post which executes asyncrhounously in one task, each handler of each subscriber for the topic spawns a task
        /// </summary>
        /// <typeparam name="M">The type of messsage argument passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="message">The single message argument passed</param>
        /// <returns>The tasks started as an array. One can the WaitAll on them if so desired</returns>
        public Task[] PostParallel<M>(T topic, M message) {
            List<Task> tasks = new List<Task>();
            if (_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                foreach (WeakReference wr in subscribers) {
                    if (wr == null || !wr.IsAlive) {
                        continue;
                    }
                    IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                    if (handlers == null) {
                        return Array.Empty<Task>();
                    }
                    foreach (Delegate handler in handlers) {
                        dynamic d = handler;
                        Task task = new Task(() => d(message));
                        tasks.Add(task);
                        task.Start(_defaultScheduler);
                    }
                }
            }
            return tasks.ToArray();
        }

        /// <summary>
        /// Parallel version of <see cref="PostAll{M}(T, M[])"/>
        /// Unlike PostAll which executes asyncrhounously in one task, each handler of each subscriber the for topic spawns a task
        /// </summary>
        /// <typeparam name="M">The type of messsages in the params array passed</typeparam>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <param name="messages">The array of messages passed</param>
        /// <returns>The tasks started as an array. One can the WaitAll on them if so desired</returns>
        public Task[] PostParallelAll<M>(T topic, params M[] messages) {
            List<Task> tasks = new List<Task>();
            if (_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                foreach (WeakReference wr in subscribers) {
                    if (wr == null || !wr.IsAlive) {
                        continue;
                    }
                    IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                    if (handlers == null) {
                        return Array.Empty<Task>();
                    }
                    foreach (Delegate handler in handlers) {
                        dynamic d = handler;
                        Task task = new Task(() => d(messages));
                        tasks.Add(task);
                        task.Start(_defaultScheduler);
                    }
                }
            }
            return tasks.ToArray();
        }

        /// <summary>
        /// Parallel version of <see cref="Post(T)"/>
        /// Unlike Post which executes asyncrhounously in one task, each handler of each subscriber the for topic spawns a task
        /// </summary>
        /// <param name="topic">The topic in which subscriber handlers might be interested in</param>
        /// <returns>he tasks started as an array. One can the WaitAll on them if so desired</returns>
        public Task[] PostParallel(T topic) {
            List<Task> tasks = new List<Task>();
            if (_subscribersByTopic.TryGetValue(topic, out List<WeakReference> subscribers)) {
                foreach (WeakReference wr in subscribers) {
                    if (wr == null || !wr.IsAlive) {
                        continue;
                    }
                    IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                    if (handlers == null) {
                        return Array.Empty<Task>();
                    }
                    foreach (Delegate handler in handlers) {
                        dynamic d = handler;
                        Task task = new Task(() => d());
                        tasks.Add(task);
                        task.Start(_defaultScheduler);
                    }
                }
            }
            return tasks.ToArray();
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
        
        private readonly TaskScheduler _defaultScheduler;
        private readonly ListDictionary<T, WeakReference> _subscribersByTopic = new ListDictionary<T, WeakReference>();
    }
}
