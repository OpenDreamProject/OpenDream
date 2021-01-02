using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace OpenDreamServer.Dream {
    class ServerIconAppearance : IconAppearance {
        private static List<ServerIconAppearance> _appearances = new();

        private int _id = -1;

        public ServerIconAppearance() : base() { }

        public ServerIconAppearance(ServerIconAppearance appearance) : base(appearance) { }

        public int GetID() {
            if (_id == -1) {
                _id = _appearances.IndexOf(this);

                if (_id == -1) {
                    _id = _appearances.Count;

                    _appearances.Add(this);
                    Program.DreamStateManager.AddIconAppearance(this);
                }
            }

            return _id;
        }

        public static ServerIconAppearance GetAppearance(int appearanceID) {
            return _appearances[appearanceID];
        }
    }
}
