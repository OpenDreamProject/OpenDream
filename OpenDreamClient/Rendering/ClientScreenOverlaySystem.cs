using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering {
    sealed class ClientScreenOverlaySystem : SharedScreenOverlaySystem {
        public HashSet<EntityUid> ScreenObjects = new();

        [Dependency] private IEntityManager _entityManager = default!;

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
