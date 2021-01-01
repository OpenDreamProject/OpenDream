using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace OpenDreamServer.Dream {
    record ServerIconAppearance : IconAppearance {
        private static List<ServerIconAppearance> _appearances = new();

        public ServerIconAppearance(ServerIconAppearance appearance) : base(appearance) { }

        public int GetID() {
            int index = _appearances.IndexOf(this);

            if (index == -1) {
                index = _appearances.Count;

                _appearances.Add(this);
                Program.DreamStateManager.AddIconAppearance(this);
            }

            return index;
        }

        public static ServerIconAppearance GetAppearance(int appearanceID) {
            return _appearances[appearanceID];
        }
    }
}
