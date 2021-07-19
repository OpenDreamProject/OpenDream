using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace OpenDreamClient {
    class DreamSystem : EntitySystem {
        public override void Initialize() {
            SubscribeLocalEvent<PlayerAttachSysMessage>(OnPlayerAttached);
        }

        private void OnPlayerAttached(PlayerAttachSysMessage message) {
            message.AttachedEntity.GetComponent<EyeComponent>().Current = true;
        }
    }
}
