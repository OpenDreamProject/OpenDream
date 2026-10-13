using System.Reflection;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using System.Buffers;
var failures = new List<string>();
void Check(string name, Action action) {
    try { action(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failures.Add(name); Console.WriteLine($"FAIL {name}: {ex.Message}"); }
}
void Equal(int expected, int actual, string label) {
    if (expected != actual) throw new Exception($"{label}: expected {expected}, got {actual}");
}
foreach (var mode in new[] { "dispose-before-visit", "early-break", "normal-exhaustion", "assignment-exception", "nonmatching-prefix", "deleted-entry", "duplicate-values" }) {
    Check("filtered/" + mode, () => {
        var first = new DreamObject { Matches = mode != "nonmatching-prefix" };
        var second = new DreamObject();
        var third = mode == "duplicate-values" ? second : new DreamObject();
        var associated = new DreamObject();
        DreamValue[] values = [new(first), new(second), new(third)];
        foreach (var value in values) value.IncRef();
        if (mode == "deleted-entry") first.Deleted = true;
        var assoc = new Dictionary<DreamValue, DreamValue> { [new(second)] = new(associated) };
        associated.IncRef();
        using var state = new DMProcState { ThrowAssignment = mode == "assignment-exception" };
        var enumerator = new FilteredDreamValueArrayEnumerator(values, assoc, new TreeEntry());
        try {
            if (mode == "normal-exhaustion") while (enumerator.Enumerate(state, DreamReference.Value, DreamReference.Association)) {}
            else if (mode != "dispose-before-visit") enumerator.Enumerate(state, DreamReference.Value, DreamReference.Association);
        } catch (InvalidOperationException) when (mode == "assignment-exception") {} finally { enumerator.Dispose(); }
        state.Dispose();
        if (!first.Deleted) Equal(1, first.RefCount, "first refcount");
        Equal(1, second.RefCount, "second refcount"); Equal(1, third.RefCount, "third refcount");
        Equal(1, associated.RefCount, "association refcount");
    });
}
Check("async/pooled-reuse-shorter-arguments", () => {
    var first = new DreamObject(); var tail = new DreamObject();
    var state = new AsyncNativeProc.AsyncNativeProcState { ProcScheduler = new ProcScheduler() };
    state.Initialize(null, _ => Task.FromResult(DreamValue.Null), new DreamThread(), null, null,
        new DreamProcArguments([new(first), new(tail)]));
    Equal(2, first.RefCount, "live first"); Equal(2, tail.RefCount, "live tail");
    state.Dispose();
    Equal(1, first.RefCount, "released first"); Equal(1, tail.RefCount, "released tail");
    var argsField = typeof(AsyncNativeProc.AsyncNativeProcState).GetField("_arguments", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var slots = (DreamValue[])argsField.GetValue(state)!;
    if (slots.Any(value => value.Object != null)) throw new Exception("pooled arguments retain managed objects");
    var reused = AsyncNativeProc.AsyncNativeProcState.Pool.Pop();
    if (!ReferenceEquals(state, reused)) throw new Exception("did not reuse state");
    for (int i = 0; i < 500; i++) {
        reused.Initialize(null, _ => Task.FromResult(DreamValue.Null), new DreamThread(), null, null, new DreamProcArguments([new(first)]));
        reused.Dispose();
        AsyncNativeProc.AsyncNativeProcState.Pool.Pop();
    }
    Equal(1, first.RefCount, "first after repeated reuse"); Equal(1, tail.RefCount, "tail after repeated reuse");
});
Check("async/stale-tail-reference-count", () => {
    var first = new DreamObject(); var tail = new DreamObject();
    var state = new AsyncNativeProc.AsyncNativeProcState { ProcScheduler = new ProcScheduler() };
    state.Initialize(null, _ => Task.FromResult(DreamValue.Null), new DreamThread(), null, null,
        new DreamProcArguments([new(first), new(tail)]));
    state.Dispose();
    AsyncNativeProc.AsyncNativeProcState.Pool.Pop();
    for (int i = 0; i < 100; i++) {
        state.Initialize(null, _ => Task.FromResult(DreamValue.Null), new DreamThread(), null, null, new DreamProcArguments());
        state.Dispose();
        AsyncNativeProc.AsyncNativeProcState.Pool.Pop();
    }
    Equal(1, first.RefCount, "first after zero-argument reuse");
    Equal(1, tail.RefCount, "tail after zero-argument reuse");
});
Check("stack/returned-array-clears-popped-and-live-slots", () => {
    var pool = ArrayPool<DreamValue>.Create(32, 1);
    var stack = pool.Rent(16);
    Array.Fill(stack, new DreamValue(new DreamObject()));
    ProductionStackReturn.Return(pool, stack);
    var reused = pool.Rent(16);
    if (!ReferenceEquals(stack, reused)) throw new Exception("pool did not reuse array");
    if (reused.Any(value => value.Object != null)) throw new Exception("returned stack retains managed objects");
});
foreach (var mode in new[] { "success", "fault", "cancel" }) {
    Check("scheduler/" + mode + "-repeated-cleanup", () => {
        var scheduler = new ProcScheduler();
        var sleepingField = typeof(ProcScheduler).GetField("_sleeping", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var sleeping = (HashSet<AsyncNativeProc.AsyncNativeProcState>)sleepingField.GetValue(scheduler)!;
        var argument = new DreamObject();
        int resumed = 0, errors = 0;
        for (int i = 0; i < 500; i++) {
            var completion = new TaskCompletionSource<DreamValue>();
            var state = new AsyncNativeProc.AsyncNativeProcState { ProcScheduler = scheduler };
            var thread = new DreamThread();
            thread.OnResume = () => {
                resumed++;
                try {
                    if (state.Resume() != ProcStatus.Returned) throw new Exception("completed task did not return");
                } catch (AggregateException) when (mode == "fault") { errors++; }
                finally { state.Dispose(); }
                return default;
            };
            state.Initialize(null, _ => completion.Task, thread, null, null, new DreamProcArguments([new(argument)]));
            if (state.Resume() != ProcStatus.Deferred) throw new Exception("pending task did not defer");
            Equal(1, sleeping.Count, "pending scheduler entry");
            if (mode == "fault") completion.SetException(new InvalidOperationException("intentional fault"));
            else if (mode == "cancel") completion.SetCanceled();
            else completion.SetResult(DreamValue.Null);
            // Completion continuations execute synchronously, matching the production scheduler contract.
            Equal(0, sleeping.Count, "completed scheduler entry");
            scheduler.Process();
            Equal(i + 1, resumed, "resumed completion count");
            Equal(1, argument.RefCount, "argument released after completion");
            AsyncNativeProc.AsyncNativeProcState.Pool.Pop();
        }
        Equal(mode == "fault" ? 500 : 0, errors, "fault delivery count");
        if (scheduler.InspectThreads().Any()) throw new Exception("scheduler retains completed threads");
    });
}
Check("scheduler/synchronous-fault-does-not-register", () => {
    var scheduler = new ProcScheduler();
    var state = new AsyncNativeProc.AsyncNativeProcState { ProcScheduler = scheduler };
    state.Initialize(null, _ => Task.FromException<DreamValue>(new InvalidOperationException("sync fault")), new DreamThread(), null, null, new DreamProcArguments());
    try { state.Resume(); throw new Exception("fault was swallowed"); }
    catch (AggregateException) {}
    finally { state.Dispose(); AsyncNativeProc.AsyncNativeProcState.Pool.Pop(); }
    if (scheduler.InspectThreads().Any()) throw new Exception("synchronous fault was registered");
});
foreach (var completeBeforeDisposal in ProbeControl.LegacyScheduler ? Array.Empty<bool>() : new[] { false, true }) {
    Check("scheduler/reused-state-ignores-old-lifetime/" + completeBeforeDisposal, () => {
        var scheduler = new ProcScheduler();
        var oldCompletion = new TaskCompletionSource<DreamValue>();
        var newCompletion = new TaskCompletionSource<DreamValue>();
        var state = new AsyncNativeProc.AsyncNativeProcState { ProcScheduler = scheduler };
        var oldThread = new DreamThread();
        oldThread.OnResume = () => throw new Exception("Disposed lifetime resumed");
        state.Initialize(null, _ => oldCompletion.Task, oldThread, null, null, new DreamProcArguments());
        state.Resume();
        if (completeBeforeDisposal) oldCompletion.SetResult(DreamValue.Null);
        state.Dispose();
        AsyncNativeProc.AsyncNativeProcState.Pool.Pop();

        int resumed = 0;
        var newThread = new DreamThread();
        newThread.OnResume = () => {
            resumed++;
            if (state.Resume() != ProcStatus.Returned) throw new Exception("New task did not complete");
            state.Dispose();
            return default;
        };
        state.Initialize(null, _ => newCompletion.Task, newThread, null, null, new DreamProcArguments());
        state.Resume();
        var abandonedResult = new DreamObject();
        if (!completeBeforeDisposal) {
            abandonedResult.IncRef();
            oldCompletion.SetResult(new DreamValue(abandonedResult));
        }
        scheduler.Process();
        Equal(0, resumed, "old completion must not resume the reused state");
        Equal(1, abandonedResult.RefCount, "old owned result must be released");
        newCompletion.SetResult(DreamValue.Null);
        scheduler.Process();
        Equal(1, resumed, "new lifetime must remain scheduled");
        AsyncNativeProc.AsyncNativeProcState.Pool.Pop();
        if (scheduler.InspectThreads().Any()) throw new Exception("Disposed lifetime retained in scheduler");
    });
}
if (!ProbeControl.LegacyScheduler) Check("scheduler/result-replacement-releases-owned-values", () => {
    foreach (var sameObject in new[] { false, true }) {
        var scheduler = new ProcScheduler();
        var previous = new DreamObject();
        var replacement = sameObject ? previous : new DreamObject();
        var completion = new TaskCompletionSource<DreamValue>();
        var state = new AsyncNativeProc.AsyncNativeProcState { ProcScheduler = scheduler };
        var thread = new DreamThread();
        thread.OnResume = () => { state.Resume(); state.Dispose(); return default; };
        state.Initialize(null, _ => completion.Task, thread, null, null, new DreamProcArguments());
        previous.IncRef();
        state.Result = new DreamValue(previous);
        state.Resume();
        replacement.IncRef();
        completion.SetResult(new DreamValue(replacement));
        Equal(sameObject ? 2 : 1, previous.RefCount, "previous result released before replacement");
        Equal(2, replacement.RefCount, "completed result owns one reference");
        scheduler.Process();
        Equal(1, previous.RefCount, "previous result fully balanced");
        Equal(1, replacement.RefCount, "replacement result fully balanced");
        AsyncNativeProc.AsyncNativeProcState.Pool.Pop();
    }
});
Check("state/dispose-clears-caller-chain", () => {
    var argument = new DreamObject();
    argument.IncRef();
    var state = new ProductionStateDispose { Thread = new object(), Caller = new object(), Result = new(argument), Id = 9, WaitFor = false };
    state.Dispose();
    if (state.Caller != null || state.Thread != null) throw new Exception("disposed state retains caller or thread");
    Equal(1, argument.RefCount, "result released");
    Equal(-1, state.Id, "disposed id");
    if (!state.WaitFor) throw new Exception("waitfor not reset");
});
if (failures.Count > 0) { Console.WriteLine($"FAILED {failures.Count} cases"); return 1; }
Console.WriteLine("Ownership probe passed. Stubbed dependencies; no game startup or live memory measurement.");
return 0;


