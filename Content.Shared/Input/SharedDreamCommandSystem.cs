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
    }
}
