# Memory ownership regression probe

Run with a verified .NET 10 SDK:

```powershell
./tests/memory_ownership/run.ps1 -Dotnet '<absolute dotnet.exe path>'
```

The runner copies the full production `DreamEnumerators.cs`, `ProcScheduler.cs`
and `AsyncNativeProc.cs` into unique temporary storage and compiles them with focused
reference-count/assignment/scheduler dependency stubs. It fully qualifies the
injection attribute solely to avoid an alias collision in the standalone build.
The stack-pool check compiles the exact `DreamValuePool.Return(_stack, ...)`
statement extracted from production `DMProc.cs` against a real .NET `ArrayPool`.
The probe has no package dependencies or engine-submodule requirement. Its
CLI home, generated source, intermediate files and binaries are temporary.

Fifteen cases cover disposing before the first enumeration, early break/return
cleanup, exhaustion, assignment exceptions, filtered-out/deleted entries,
duplicate snapshot values, association ownership, shorter/empty argument reuse,
and clearing every slot of returned stack arrays. Snapshot increments mirror
`GetContentsEnumerator`; assignment stubs increment loop-variable ownership.
The scheduler uses the production async state and scheduling code, with inert
delay processing and a thread adapter that exercises completion/error disposal.
It runs 500 success, fault and cancellation cycles each, checking that sleeping
entries disappear, completion resumes once, and argument ownership is released.
Synchronous faults must not enter the sleeping set. The exact base-state Dispose
body is also compiled to check that pooled states clear their caller links.
Objects retain an independent initial reference, making extra increments and
unbalanced decrements directly observable.

Negative controls must compile and fail with ownership assertions:

```powershell
./tests/memory_ownership/run.ps1 -Dotnet '<absolute dotnet.exe path>' -NegativeControl baseline
./tests/memory_ownership/run.ps1 -Dotnet '<absolute dotnet.exe path>' -NegativeControl filtered-dispose
./tests/memory_ownership/run.ps1 -Dotnet '<absolute dotnet.exe path>' -NegativeControl async-clear
./tests/memory_ownership/run.ps1 -Dotnet '<absolute dotnet.exe path>' -NegativeControl stack-clear
./tests/memory_ownership/run.ps1 -Dotnet '<absolute dotnet.exe path>' -NegativeControl scheduler-fault
./tests/memory_ownership/run.ps1 -Dotnet '<absolute dotnet.exe path>' -NegativeControl caller-clear
```

`baseline` uses immutable engine commit `b1a452e7`. The other controls remove
one cleanup operation only in generated temporary source, except the scheduler
control which restores its pre-audit source. Baseline reproduction
shows an async argument's reference count increasing from 1 to 101 after 100
empty-argument reuses, while the corrected source leaves it at 1. Normal filtered
exhaustion already passes baseline; early exits fail baseline. The scheduler
negative control uses immutable pre-audit source `d443a95f`; it fails both fault
and cancellation cleanup. Removing the caller reset fails its disposal check.

This verifies production ownership operations with isolated dependencies. It
is not a complete engine build, DM compiler integration test, gameplay test,
server heap measurement or proof that these defects caused Civ13's observed heap
size. No live runtime is attached. Existing nullable-analysis warnings in linked
async code are reported by the standalone build.

## Passive Windows server benchmark

`benchmark-process.ps1 -TargetProcessId <PID> -CounterTool <dotnet-counters.exe>
-OutputDirectory <new directory> -Seconds 120` records process memory, CPU time
and CLR counters. Optional PythonExecutable/ControlScript/ControlProfile arguments
poll an explicitly selected server's read-only `topic-status`; ServerLog records
only the new log window. It refuses to overwrite existing counter output and
checks process start identity. The server must already be owned and authorized.
The collector performs no restart, season transition or forced GC.

`summarize-process.py <directory> [<directory> ...] --output <summary.json>`
summarizes the raw CSV files. Topic latency includes process launch overhead;
game clock advance is not a frame-time measurement. Compare workload/player
count, process age, initialization and GC generations before attributing any
change to the patch. A fresh process uses less memory even without a leak fix.
