using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Input {
    [Virtual]
    public class SharedDreamCommandSystem : EntitySystem {
        [Serializable, NetSerializable]
        public sealed class CommandEvent : EntityEventArgs {
            public string Command { get; }

            public CommandEvent(string command) {
                Command = command;
            }
        }

        [Serializable, NetSerializable]
        public sealed class RepeatCommandEvent : EntityEventArgs {
            public string Command { get; }

            public RepeatCommandEvent(string command) {
                Command = command;
            }
        }

        [Serializable, NetSerializable]
        public sealed class StopRepeatCommandEvent : EntityEventArgs {
            public string Command { get; }

            public StopRepeatCommandEvent(string command) {
                Command = command;
            }
        }
    }
}
