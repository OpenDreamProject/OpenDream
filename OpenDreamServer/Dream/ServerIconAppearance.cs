using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamServer.Dream {
    class ServerIconAppearance : IconAppearance {
        private static Dictionary<ServerIconAppearance, int> _appearanceToID = new();

        public ServerIconAppearance() : base() { }

        public ServerIconAppearance(ServerIconAppearance appearance) : base(appearance) { }

        public int GetID() {
            int appearanceID;

            if (!_appearanceToID.TryGetValue(this, out appearanceID)) {
                appearanceID = _appearanceToID.Count;

                _appearanceToID.Add(this, appearanceID);
                Program.DreamStateManager.AddIconAppearance(this);
            }

            return appearanceID;
        }
    }
}
