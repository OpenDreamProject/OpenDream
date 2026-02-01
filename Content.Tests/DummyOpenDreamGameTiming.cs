using OpenDreamRuntime.Procs;

using Robust.Shared.IoC;
using Robust.Shared.Timing;

using System;

namespace Content.Tests {
    sealed class DummyOpenDreamGameTiming : IOpenDreamGameTiming {

        [Dependency] IGameTiming _gameTiming = null!;

        public GameTick CurTick { get; set; }

        public TimeSpan LastTick => _gameTiming.LastTick;

        public TimeSpan RealTime => _gameTiming.RealTime;

        public TimeSpan TickPeriod => _gameTiming.TickPeriod;

        public ushort TickRate {
            get => _gameTiming.TickRate;
            set => _gameTiming.TickRate = value;
        }
    }
}
