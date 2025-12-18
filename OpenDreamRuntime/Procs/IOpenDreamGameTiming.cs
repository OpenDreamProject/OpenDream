using Robust.Shared.Timing;

namespace OpenDreamRuntime.Procs {
    public interface IOpenDreamGameTiming {
        public GameTick CurTick { get; }

        public TimeSpan LastTick { get; }

        public TimeSpan RealTime { get; }

        public TimeSpan TickPeriod { get; }

        public ushort TickRate { get; set; }
    }
}
