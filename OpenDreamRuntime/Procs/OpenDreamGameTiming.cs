using Robust.Shared.Timing;

namespace OpenDreamRuntime.Procs;

internal sealed partial class OpenDreamGameTiming : IOpenDreamGameTiming {
    [Dependency] private IGameTiming _gameTiming = default!;

    public GameTick CurTick => _gameTiming.CurTick;

    public TimeSpan LastTick => _gameTiming.LastTick;

    public TimeSpan RealTime => _gameTiming.RealTime;

    public TimeSpan TickPeriod => _gameTiming.TickPeriod;

    public ushort TickRate {
        get => _gameTiming.TickRate;
        set => _gameTiming.TickRate = value;
    }
}
