using System.Threading.Tasks;
using Robust.Shared.Timing;

namespace OpenDreamRuntime.Procs;

// Handles delay processing for sleep() and spawn().

public sealed partial class ProcScheduler {
    private sealed class ThreadYieldTracker {
        public uint ThreadYieldCounter = 1;
        public readonly Dictionary<int, uint> ProcYieldCounter = new();
    }

    [Dependency] private readonly IOpenDreamGameTiming _gameTiming = default!;

    private PriorityQueue<DelayTicker, uint> _tickers = new();

    private readonly Dictionary<int, ThreadYieldTracker> _yieldTrackersByThread = new();

    private readonly ushort _yieldProcThreshold = 10;
    private readonly ushort _yieldThreadThreshold = 20;

    // This is for deferred tasks that need to fire in the current tick.
    private readonly Queue<TaskCompletionSource> _deferredTasks = new();

    public bool HasProcsSleeping => _tickers.Count > 0;
    /// <summary>
    /// Create a task that will delay by an amount of time, following the rules for <c>sleep</c> and <c>spawn</c>.
    /// </summary>
    /// <param name="deciseconds">
    /// The amount of time, in deciseconds, to sleep. Gets rounded down to a number of ticks.
    /// </param>
    /// <param name="procId">
    /// ID of active process to track sharing
    /// </param>
    /// <param name="threadId">
    /// ID of active thread to track sharing
    /// </param>
    public Task CreateDelay(float deciseconds, int procId, int threadId) {
        // BYOND stores sleep/spawn delays with an exact amount of ticks.
        // Yes, this means that if you change world.fps/tick_lag while sleeping,
        // those sleep delays can speed up/slow down. We're replicating that here.
        var periodDs = _gameTiming.TickPeriod.TotalSeconds * 10;
        var countTicks = (int)(deciseconds / periodDs);

        // Anything above 0 deciseconds should be at least 1 tick
        countTicks = (deciseconds > 0f) ? Math.Max(countTicks, 1) : countTicks;

        return CreateDelayTicks(countTicks, procId, threadId);
    }

    /// <summary>
    /// Create a task that will delay by an amount of game ticks
    /// </summary>
    /// <param name="ticks">
    /// The amount of ticks to sleep.
    /// </param>
    /// <param name="procId">
    /// ID of active process to track sharing
    /// </param>
    /// <param name="threadId">
    /// ID of active thread to track sharing
    /// </param>
    public Task CreateDelayTicks(int ticks, int procId, int threadId) {
        // When the delay is <= zero, we should run again in the current tick.
        // Now, BYOND apparently does have a difference between 0 and -1. See https://github.com/OpenDreamProject/OpenDream/issues/1262#issuecomment-1563663041
        // 0 delays until all other sleeping procs have had a chance to resume if their sleep time has elapsed.
        // We achieve this by putting the proc on the _deferredTasks lists, so it can be immediately executed again.
        // -1 yields "only if other pending events have become backlogged" according to the BYOND docs.
        // In simple testing, this tends to amount to the same as one thread sleeping with -1 twenty times in one tick or a single proc ten times.

        if (ticks < 0) {
            var yieldTracker = _yieldTrackersByThread.GetValueOrDefault(threadId, new());
            uint sleepCountByThread = yieldTracker.ThreadYieldCounter;
            uint sleepCountByProc = yieldTracker.ProcYieldCounter.GetValueOrDefault(procId, 1u);

            bool exceeded = false;
            if (sleepCountByThread < _yieldThreadThreshold) {
                yieldTracker.ThreadYieldCounter = sleepCountByThread + 1u;
            } else {
                exceeded = true;
            }

            if (sleepCountByProc < _yieldProcThreshold) {
                yieldTracker.ProcYieldCounter[procId] = sleepCountByProc + 1u;
            } else {
                exceeded = true;
            }

            if (exceeded) {
                _yieldTrackersByThread.Remove(threadId);
            } else {
                _yieldTrackersByThread[threadId] = yieldTracker;
                return Task.CompletedTask;
            }
        }

        if (ticks <= 0) {
            var defTcs = new TaskCompletionSource();
            _deferredTasks.Enqueue(defTcs);
            return defTcs.Task;
        }

        var tcs = new TaskCompletionSource();

        InsertTask(new DelayTicker(tcs) { TicksAt = _gameTiming.CurTick.Value + (uint)ticks }); //safe cast because ticks is always positive here
        return tcs.Task;
    }


    /// <summary>
    /// Insert a ticker into the queue to maintain sorted order
    /// </summary>
    /// <param name="ticker"></param>
    private void InsertTask(DelayTicker ticker) {
        _tickers.Enqueue(ticker, ticker.TicksAt);
    }

    private void UpdateDelays() {
        _yieldTrackersByThread.Clear();

        while (_tickers.Count > 0) {
            var ticker = _tickers.Peek();
            if(ticker.TicksAt > _gameTiming.CurTick.Value)
                break; //queue is sorted, so if we hit a ticker that isn't ready, we can stop
            ticker.TaskCompletionSource.TrySetResult();
            _tickers.Dequeue();
        }
    }

    private sealed class DelayTicker {
        public readonly TaskCompletionSource TaskCompletionSource;
        public required uint TicksAt;

        public DelayTicker(TaskCompletionSource taskCompletionSource) {
            TaskCompletionSource = taskCompletionSource;
        }
    }
}
