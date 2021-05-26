using OpenDreamShared.Dream;

namespace OpenDreamRuntime {
    public class ServerIconAppearance : IconAppearance {
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

            if (!Runtime.AppearanceToID.TryGetValue(this, out appearanceID)) {
                appearanceID = Runtime.AppearanceToID.Count;

                Runtime.AppearanceToID.Add(this, appearanceID);
                Runtime.StateManager.AddIconAppearance(this);
            }

            return appearanceID;
        }
    }
}
