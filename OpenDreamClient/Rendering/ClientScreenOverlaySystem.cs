using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace OpenDreamClient.Rendering {
    class ClientScreenOverlaySystem : SharedScreenOverlaySystem {
        public List<EntityUid> ScreenObjects = new();

        public override void Initialize() {
            SubscribeNetworkEvent<AddScreenObjectEvent>(OnAddScreenObject);
            SubscribeNetworkEvent<RemoveScreenObjectEvent>(OnRemoveScreenObject);
        }

        public override void Shutdown() {
            ScreenObjects.Clear();
        }

        private void OnAddScreenObject(AddScreenObjectEvent e) {
            ScreenObjects.Add(e.ScreenObject);
        }

        private void OnRemoveScreenObject(RemoveScreenObjectEvent e) {
            ScreenObjects.Remove(e.ScreenObject);
        }
    }
}
