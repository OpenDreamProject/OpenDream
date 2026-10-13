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
    var state = new AsyncNativeProc.AsyncNativeProcState();
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
    var state = new AsyncNativeProc.AsyncNativeProcState();
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
if (failures.Count > 0) { Console.WriteLine($"FAILED {failures.Count} cases"); return 1; }
Console.WriteLine("Ownership probe passed. Stubbed dependencies; no game startup or live memory measurement.");
return 0;


