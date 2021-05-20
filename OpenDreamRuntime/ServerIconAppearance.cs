using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamVM {
    public class ServerIconAppearance : IconAppearance {
        // TODO: global state
        private static Dictionary<ServerIconAppearance, int> _appearanceToID = new();

        public DreamRuntime Runtime { get; }

        public ServerIconAppearance(DreamRuntime runtime) : base() {
            Runtime = runtime;
        }

        public ServerIconAppearance(DreamRuntime runtime, ServerIconAppearance appearance)
            : base(appearance)
        {
            Runtime = runtime;
        }

        public int GetID() {
            int appearanceID;

            if (!_appearanceToID.TryGetValue(this, out appearanceID)) {
                appearanceID = _appearanceToID.Count;

                _appearanceToID.Add(this, appearanceID);
                Runtime.StateManager.AddIconAppearance(this);
            }

            return appearanceID;
        }
    }
}
