using System.Threading.Tasks;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace OpenDreamRuntime.Procs;

// Handles delay processing for sleep() and spawn().

public sealed partial class ProcScheduler {
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private SortedList<DelayTicker, uint> _tickers = new();

    // This is for deferred tasks that need to fire in the current tick.
    private readonly Queue<TaskCompletionSource> _deferredTasks = new();

    /// <summary>
    /// Create a task that will delay by an amount of time, following the rules for <c>sleep</c> and <c>spawn</c>.
    /// </summary>
    /// <param name="deciseconds">
    /// The amount of time, in deciseconds, to sleep. Gets rounded down to a number of ticks.
    /// </param>
    public Task CreateDelay(float deciseconds) {
        // BYOND stores sleep/spawn delays with an exact amount of ticks.
        // Yes, this means that if you change world.fps/tick_lag while sleeping,
        // those sleep delays can speed up/slow down. We're replicating that here.
        var periodDs = _gameTiming.TickPeriod.TotalSeconds * 10;
        var countTicks = (int)(deciseconds / periodDs);

        // Anything above 0 deciseconds should be at least 1 tick
        countTicks = (deciseconds > 0f) ? Math.Max(countTicks, 1) : countTicks;

        return CreateDelayTicks(countTicks);
    }

    /// <summary>
    /// Create a task that will delay by an amount of game ticks
    /// </summary>
    /// <param name="ticks">
    /// The amount of ticks to sleep.
    /// </param>
    public Task CreateDelayTicks(int ticks) {
        if (ticks <= 0) {
            // When the delay is <= zero, we should run again in the current tick.
            // Now, BYOND apparently does have a difference between 0 and -1, but we're not quite sure what it is yet.
            // This is "good enough" for now.
            // They both delay execution and allow other sleeping procs in the current tick to run immediately.
            // We achieve this by putting the proc on the _deferredTasks lists, so it can be immediately executed again.

            var defTcs = new TaskCompletionSource();
            _deferredTasks.Enqueue(defTcs);
            return defTcs.Task;
        }

        var tcs = new TaskCompletionSource();
        var newTicker = new DelayTicker(tcs) { TicksAt = _gameTiming.CurTick.Value + (uint)ticks }; //safe cast because ticks is always positive here
        _tickers.Add(newTicker, newTicker.TicksAt);
        return tcs.Task;
    }

    /// <summary>
    /// binary insertion of a ticker into the list to maintain sorted order
    /// </summary>
    /// <param name="ticker"></param>


    private void UpdateDelays() {
        var i = 0;
        while(i < _tickers.Count) {
            var ticker = _tickers.GetKeyAtIndex(i);
            if(ticker.TicksAt > _gameTiming.CurTick.Value)
                break; //list is sorted, so if we hit a ticker that isn't ready, we can stop
            ticker.TaskCompletionSource.TrySetResult();
            i++;
        }
        //remove the ones we processed
        while(i > 0)
            _tickers.RemoveAt(--i);
    }

    private sealed class DelayTicker : IComparable{
        public readonly TaskCompletionSource TaskCompletionSource;
        public uint TicksAt;

        public int CompareTo(DelayTicker other){
            return this.TicksAt - other.TicksAt;
        }

        public DelayTicker(TaskCompletionSource taskCompletionSource) {
            TaskCompletionSource = taskCompletionSource;
        }
    }
}
