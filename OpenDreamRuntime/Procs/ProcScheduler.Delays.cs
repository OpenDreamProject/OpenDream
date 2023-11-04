using System.Threading.Tasks;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace OpenDreamRuntime.Procs;

// Handles delay processing for sleep() and spawn().

public sealed partial class ProcScheduler {
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private ValueList<DelayTicker> _tickers;

    // This is for deferred tasks that need to fire in the current tick.
    private readonly Queue<TaskCompletionSource> _deferredTasks = new();

    /// <summary>
    /// Create a task that will delay by an amount of time, following the rules for <c>sleep</c> and <c>spawn</c>.
    /// </summary>
    /// <param name="deciseconds">
    /// The amount of time, in deciseconds, to sleep. Gets rounded down to a number of ticks.
    /// </param>
    public Task CreateDelay(float deciseconds) {
        if (deciseconds <= 0) {
            // When the delay is <= zero, we should run again in the current tick.
            // Now, BYOND apparently does have a difference between 0 and -1, but we're not quite sure what it is yet.
            // This is "good enough" for now.
            // They both delay execution and allow other sleeping procs in the current tick to run immediately.
            // We achieve this by putting the proc on the _deferredTasks lists, so it can be immediately executed again.

            var defTcs = new TaskCompletionSource();
            _deferredTasks.Enqueue(defTcs);
            return defTcs.Task;
        }

        // BYOND stores sleep/spawn delays with an exact amount of ticks.
        // Yes, this means that if you change world.fps/tick_lag while sleeping,
        // those sleep delays can speed up/slow down. We're replicating that here.
        var periodDs = _gameTiming.TickPeriod.TotalSeconds * 10;
        var countTicks = (int)(deciseconds / periodDs);

        var tcs = new TaskCompletionSource();
        _tickers.Add(new DelayTicker(tcs) { TicksLeft = countTicks + 1 }); // Add 1 because it'll get decreased at the end of this tick
        return tcs.Task;
    }

    private void UpdateDelays() {
        // TODO: This is O(n) every tick for the amount of delays we have.
        // It may be possible to optimize this.
        for (var i = 0; i < _tickers.Count; i++) {
            var ticker = _tickers[i];
            ticker.TicksLeft -= 1;
            if (ticker.TicksLeft != 0)
                continue;

            ticker.TaskCompletionSource.TrySetResult();
            _tickers.RemoveSwap(i);
            i -= 1;
        }
    }

    private sealed class DelayTicker {
        public readonly TaskCompletionSource TaskCompletionSource;
        public int TicksLeft;

        public DelayTicker(TaskCompletionSource taskCompletionSource) {
            TaskCompletionSource = taskCompletionSource;
        }
    }
}
