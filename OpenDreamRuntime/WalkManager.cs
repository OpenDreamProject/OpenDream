using System.Threading;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamShared.Dream;
using Robust.Shared.Timing;

namespace OpenDreamRuntime;

/// <summary>
/// Handles walking movables.<br/>
/// walk_towards(), walk_to(), walk_away(), etc.
/// </summary>
public sealed class WalkManager {
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
    [Dependency] private readonly ProcScheduler _scheduler = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly Dictionary<DreamObjectMovable, CancellationTokenSource> _walkTasks = new();

    public void StopWalks(DreamObjectMovable movable) {
        if (_walkTasks.TryGetValue(movable, out var walk))
            walk.Cancel();
    }

    /// <summary>
    /// Walk towards the target with no pathfinding taken in account
    /// </summary>
    public void StartWalkTowards(DreamObjectMovable movable, DreamObjectAtom target, int lag) {
        StopWalks(movable);

        float lagDeciseconds = (Math.Max(lag, 1) * _gameTiming.TickPeriod).Milliseconds / 100f;

        CancellationTokenSource cancelSource = new();
        _walkTasks[movable] = cancelSource;

        DreamThread.Run("walk_towards", async state => {
            var moveProc = movable.GetProc("Move");

            while (true) {
                await _scheduler.CreateDelay(lagDeciseconds);
                if (cancelSource.IsCancellationRequested)
                    break;

                AtomDirection dir = DreamProcNativeHelpers.GetDir(_atomManager, movable, target);
                if (dir == AtomDirection.None)
                    continue;

                DreamObjectTurf? newLoc = DreamProcNativeHelpers.GetStep(_atomManager, _dreamMapManager, movable, dir);
                await state.Call(moveProc, movable, null, new(newLoc), new((int)dir));
            }

            return DreamValue.Null;
        });
    }
}
