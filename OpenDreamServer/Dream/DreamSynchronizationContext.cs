using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream {
    class DreamSynchronizationContext : SynchronizationContext {
        private ConcurrentQueue<(SendOrPostCallback, object)> _queue = new();

        public override void Post(SendOrPostCallback d, object state)
        {
            _queue.Enqueue((d, state));
        }

        public void Process() {
            while (_queue.TryDequeue(out var work)) {
                var (d, state) = work;
                d(state);
            }
        }
    }

    class DreamTaskScheduler : TaskScheduler
    {
        private DreamSynchronizationContext _context = new();
        private List<Task> _tasks = new();

        public DreamTaskScheduler() {
            SynchronizationContext.SetSynchronizationContext(_context);
        }

        public void TryImmediate(Task task) {
            if (!TryExecuteTaskInline(task, true)) {
                throw new InvalidOperationException();
            }
        }

        public void Process() {
            int index = 0;

            while (true) {
                // `_tasks` can't remain locked during execution
                Task task;

                lock (_tasks) {
                    if (_tasks.Count <= index) {
                        break;
                    }

                    task = _tasks[index++];
                }

                if (task != null) {
                    if (!TryExecuteTask(task)) {
                        throw new InvalidOperationException();
                    }
                }
            }

            lock (_tasks) {
                _tasks.RemoveRange(0, index);
            }

            _context.Process();
        }

        protected override IEnumerable<Task> GetScheduledTasks() {
            lock (_tasks) {
                foreach(var task in _tasks) {
                    yield return task;
                }
            }
        }

        protected override void QueueTask(Task task) {
            lock (_tasks) {
                _tasks.Add(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            if (_context != SynchronizationContext.Current) {
                return false;
            }

            return TryExecuteTask(task);
        }
    }
}