using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Input {
    public class SharedDreamCommandSystem : EntitySystem {
        [Serializable, NetSerializable]
        public class CommandEvent : EntityEventArgs {
            public string Command { get; }

            public CommandEvent(string command) {
                Command = command;
            }
        }

        [Serializable, NetSerializable]
        public class RepeatCommandEvent : EntityEventArgs {
            public string Command { get; }

            public RepeatCommandEvent(string command) {
                Command = command;
            }
        }

        [Serializable, NetSerializable]
        public class StopRepeatCommandEvent : EntityEventArgs {
            public string Command { get; }

            public StopRepeatCommandEvent(string command) {
                Command = command;
            }
        }
    }
}
