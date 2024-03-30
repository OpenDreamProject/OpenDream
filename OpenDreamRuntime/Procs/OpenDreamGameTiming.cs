using Robust.Shared.Timing;

namespace OpenDreamRuntime.Procs {
    sealed class OpenDreamGameTiming : IOpenDreamGameTiming {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public GameTick CurTick => _gameTiming.CurTick;

        public TimeSpan LastTick => _gameTiming.LastTick;

        public TimeSpan RealTime => _gameTiming.RealTime;

        public TimeSpan TickPeriod => _gameTiming.TickPeriod;

        public byte TickRate {
            get => _gameTiming.TickRate;
            set => _gameTiming.TickRate = value;
        }
    }
}
