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