#if NET40
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Enima.Utils {
    public class Mediator40<T> : Mediator<T>, IMediatorAsync<T> {
        public Mediator40() {
            _defaultScheduler = TaskScheduler.Default;
        }
        
        /// <summary>
        /// Asynchronous version of <see cref="Send{T1}(T, T1)"/>
        /// Starts a task with the TaskScheduler.Default scheduler to call <see cref="Send{T1}(T, T1)"/>
        /// </summary>
        public Task Post<T1>(T topic, T1 arg) {
            Task task = new Task(() => {
                Send(topic, arg);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        /// <summary>
        /// Asynchronous version of <see cref="SendAll{T1}(T, T1[])"/>
        /// Starts a task with the TaskScheduler.Default scheduler to call <see cref="SendAll{T1}(T, T1[])"/>
        /// </summary>
        public Task PostAll<T1>(T topic, params T1[] args) {
            Task task = new Task(() => {
                SendAll(topic, args);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        /// <summary>
        /// Asyncrhonous version of <see cref="Send(T)"/>
        /// Starts a task with the TaskScheduler.Default scheduler to call <see cref="Send(T)"/>
        /// </summary>
        public Task Post(T topic) {
            Task task = new Task(() => {
                Send(topic);
            });
            task.Start(_defaultScheduler);
            return task;
        }

        /// <summary>
        /// Parallel version of <see cref="Post{T1}(T, T1)"/>
        /// Unlike Post which executes asyncrhounously in one task, each handler of each subscriber for the topic spawns a task
        /// </summary>
        public Task[] PostParallel<T1>(T topic, T1 arg) {
            List<Task> tasks = new List<Task>();
            List<WeakReference> subscribers;
            if (_subscribersByTopic.TryGetValue(topic, out subscribers)) {
                for (int i = 0; i < subscribers.Count; i++) {
                    WeakReference wr = subscribers[i];
                    if (wr == null || !wr.IsAlive) {
                        continue;
                    }
                    IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                    if (handlers == null) {
                        return Array.Empty<Task>();
                    }
                    for (int j = 0; j < handlers.Count; j++) {
                        dynamic d = handlers[j];
                        Task task = new Task(() => d(arg));
                        tasks.Add(task);
                        task.Start(_defaultScheduler);
                    }
                }
            }
            return tasks.ToArray();
        }

        /// <summary>
        /// Parallel version of <see cref="PostAll{T1}(T, T1[])"/>
        /// Unlike PostAll which executes asyncrhounously in one task, each handler of each subscriber the for topic spawns a task
        /// </summary>
        public Task[] PostParallelAll<T1>(T topic, params T1[] args) {
            List<Task> tasks = new List<Task>();
            List<WeakReference> subscribers;
            if (_subscribersByTopic.TryGetValue(topic, out subscribers)) {
                for (int i = 0; i < subscribers.Count; i++) {
                    WeakReference wr = subscribers[i];
                    if (wr == null || !wr.IsAlive) {
                        continue;
                    }
                    IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                    if (handlers == null) {
                        return Array.Empty<Task>();
                    }
                    for (int j = 0; j < handlers.Count; j++) {
                        dynamic d = handlers[j];
                        Task task = new Task(() => d(args));
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
        public Task[] PostParallel(T topic) {
            List<Task> tasks = new List<Task>();
            List<WeakReference> subscribers;
            if (_subscribersByTopic.TryGetValue(topic, out subscribers)) {
                for (int i = 0; i < subscribers.Count; i++) {
                    WeakReference wr = subscribers[i];
                    if (wr == null || !wr.IsAlive) {
                        continue;
                    }
                    IList<Delegate> handlers = ((ISubscriber<T>)wr.Target).GetHandlers(topic);
                    if (handlers == null) {
                        return Array.Empty<Task>();
                    }
                    for (int j = 0; j < handlers.Count; j++) {
                        dynamic d = handlers[j];
                        Task task = new Task(() => d());
                        tasks.Add(task);
                        task.Start(_defaultScheduler);
                    }
                }
            }
            return tasks.ToArray();
        }

        private readonly TaskScheduler _defaultScheduler;
    }
}
#endif