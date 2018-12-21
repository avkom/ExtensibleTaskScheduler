using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Threading.Contrib
{
    public class ExtensibleTaskScheduler : TaskScheduler
    {
        private readonly ITaskSchedulerExtension _taskSchedulerExtension;

        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed 
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

        public ExtensibleTaskScheduler(ITaskSchedulerExtension taskSchedulerExtension)
        {
            _taskSchedulerExtension = taskSchedulerExtension;
        }

        // Queues a task to the scheduler. 
        protected override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough 
            // delegates currently queued or running to process tasks, schedule another. 
            lock (_tasks)
            {
                _tasks.AddLast(task);
                NotifyThreadPoolOfPendingWork();
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler. 
        private void NotifyThreadPoolOfPendingWork()
        {
            object contextFromParentThread =_taskSchedulerExtension.GetContextFromParentThread();

            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasks.Count == 0)
                            {
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        object contextFromChildThread = null;

                        try
                        {
                            contextFromChildThread =_taskSchedulerExtension.EnterChildThread(contextFromParentThread);

                            // Execute the task we pulled out of the queue
                            base.TryExecuteTask(item);
                        }
                        finally
                        {
                            _taskSchedulerExtension.ExitChildThread(contextFromParentThread, contextFromChildThread);
                        }
                    }
                }
                // We're done processing items on the current thread
                finally
                {
                    _currentThreadIsProcessingItems = false;
                }
            }, null);
        }

        // Attempts to execute the specified task on the current thread. 
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued)
                // Try to run the task. 
                if (TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        // Attempt to remove a previously scheduled task from the scheduler. 
        protected override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        // Gets an enumerable of the tasks currently scheduled on this scheduler. 
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }
}
