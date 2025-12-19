using System.Linq;
using System.Threading;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime;

/// <summary>
/// Handles walking movables.<br/>
/// walk_towards(), walk_to(), walk_away(), etc.
/// </summary>
public sealed class WalkManager {
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
    [Dependency] private readonly ProcScheduler _scheduler = default!;
    [Dependency] private readonly DreamManager _dreamManager = default!;

    private readonly Dictionary<DreamObjectMovable, CancellationTokenSource> _walkTasks = new();

    /// <summary>
    /// Stop any active walks on a movable
    /// </summary>
    public void StopWalks(DreamObjectMovable movable) {
        if (_walkTasks.TryGetValue(movable, out var walk))
            walk.Cancel();
    }

    /// <summary>
    /// Walk in the specified direction Dir continuously.
    /// </summary>
    public void StartWalk(DreamObjectMovable movable, int dir, int lag, int speed) { // TODO: Implement speed. Speed=0 uses Ref.step_size
        StopWalks(movable);

        lag = Math.Max(lag, 1); // Minimum of 1 tick lag

        CancellationTokenSource cancelSource = new();
        _walkTasks[movable] = cancelSource;

        DreamThread.Run($"walk {dir}", async state => {
            var moveProc = movable.GetProc("Move");

            while (true) {
                await _scheduler.CreateDelayTicks(lag, state.Proc.Id, state.Thread.Id);
                if (cancelSource.IsCancellationRequested)
                    break;

                DreamObjectTurf? newLoc = DreamProcNativeHelpers.GetStep(_atomManager, _dreamMapManager, movable, (AtomDirection)dir);
                await state.Call(moveProc, movable, null, new(newLoc), new(dir));
            }

            return DreamValue.Null;
        });
    }

    /// <summary>
    /// Walk in a random direction continuously.
    /// </summary>
    public void StartWalkRand(DreamObjectMovable movable, int lag, int speed) { // TODO: Implement speed. Speed=0 uses Ref.step_size
        StopWalks(movable);

        lag = Math.Max(lag, 1); // Minimum of 1 tick lag

        CancellationTokenSource cancelSource = new();
        _walkTasks[movable] = cancelSource;

        DreamThread.Run("walk_rand", async state => {
            var moveProc = movable.GetProc("Move");

            while (true) {
                await _scheduler.CreateDelayTicks(lag, state.Proc.Id, state.Thread.Id);
                if (cancelSource.IsCancellationRequested)
                    break;
                var dir = DreamProcNativeHelpers.GetRandomDirection(_dreamManager);
                DreamObjectTurf? newLoc = DreamProcNativeHelpers.GetStep(_atomManager, _dreamMapManager, movable, dir);
                await state.Call(moveProc, movable, null, new(newLoc), new((int)dir));
            }

            return DreamValue.Null;
        });
    }

    /// <summary>
    /// Walk towards the target with no pathfinding taken into account
    /// </summary>
    public void StartWalkTowards(DreamObjectMovable movable, DreamObjectAtom target, int lag, int speed) { // TODO: Implement speed. Speed=0 uses Ref.step_size
        StopWalks(movable);

        lag = Math.Max(lag, 1); // Minimum of 1 tick lag

        CancellationTokenSource cancelSource = new();
        _walkTasks[movable] = cancelSource;

        DreamThread.Run($"walk_towards {movable}", async state => {
            var moveProc = movable.GetProc("Move");

            while (true) {
                await _scheduler.CreateDelayTicks(lag, state.Proc.Id, state.Thread.Id);
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

    /// <summary>
    /// Walk towards the target with pathfinding taken into account
    /// </summary>
    public void StartWalkTo(DreamObjectMovable movable, DreamObjectAtom target, int min, int lag, int speed) { // TODO: Implement speed. Speed=0 uses Ref.step_size
        StopWalks(movable);

        lag = Math.Max(lag, 1); // Minimum of 1 tick lag

        CancellationTokenSource cancelSource = new();
        _walkTasks[movable] = cancelSource;

        DreamThread.Run($"walk_to {movable}", async state => {
            var moveProc = movable.GetProc("Move");

            while (true) {
                await _scheduler.CreateDelayTicks(lag, state.Proc.Id, state.Thread.Id);
                if (cancelSource.IsCancellationRequested)
                    break;

                var currentLoc = _atomManager.GetAtomPosition(movable);
                var targetLoc = _atomManager.GetAtomPosition(target);
                var steps = _dreamMapManager.CalculateSteps(currentLoc, targetLoc, min);
                using var enumerator = steps.GetEnumerator();
                if (!enumerator.MoveNext()) // No more steps to take
                    break;

                var dir = enumerator.Current;
                var newLoc = DreamProcNativeHelpers.GetStep(_atomManager, _dreamMapManager, movable, dir);
                await state.Call(moveProc, movable, null, new(newLoc), new((int)dir));
            }

            return DreamValue.Null;
        });
    }
}
