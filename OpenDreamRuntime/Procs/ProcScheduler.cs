namespace OpenDreamRuntime.Procs {
    sealed class ProcScheduler : IProcScheduler {
        private Queue<AsyncNativeProc.State> _scheduled = new();

        public void ScheduleAsyncNative(AsyncNativeProc.State state) {
            _scheduled.Enqueue(state);
        }

        public void Process() {
            while (_scheduled.TryDequeue(out var queued)) {
                queued.SafeResume();
            }
        }
    }

    interface IProcScheduler {
        public void ScheduleAsyncNative(AsyncNativeProc.State state);

        public void Process();
    }
}
