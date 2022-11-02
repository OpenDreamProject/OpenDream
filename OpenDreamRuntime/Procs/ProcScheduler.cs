using System.Threading.Tasks;

namespace OpenDreamRuntime.Procs {
    sealed class ProcScheduler : IProcScheduler {
        private HashSet<AsyncNativeProc.State> _sleeping = new();
        private Queue<AsyncNativeProc.State> _scheduled = new();
        private AsyncNativeProc.State? _current;

        public void ScheduleAsyncNative(AsyncNativeProc.State state, Task task) {
            _sleeping.Add(state);
            task.ContinueWith(
                _ => {
                    _sleeping.Remove(state);
                    _scheduled.Enqueue(state);
                },
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        public void Process() {
            while (_scheduled.TryDequeue(out _current)) {
                _current.SafeResume();
            }
        }

        public IEnumerable<DreamThread> InspectThreads() {
            if (_current is not null) {
                yield return _current.Thread;
            }
            foreach (var state in _scheduled) {
                yield return state.Thread;
            }
            foreach (var state in _sleeping) {
                yield return state.Thread;
            }
        }
    }

    interface IProcScheduler {
        public void ScheduleAsyncNative(AsyncNativeProc.State state, Task task);

        public void Process();

        public IEnumerable<DreamThread> InspectThreads();
    }
}
