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

        public IEnumerable<DMISpriteComponent> EnumerateScreenObjects() {
            foreach (EntityUid uid in ScreenObjects) {
                if (_entityManager.TryGetComponent(uid, out DMISpriteComponent sprite)) {
                    if (sprite.ScreenLocation == null) continue;

                    yield return sprite;
                }
            }
        }

        private void OnAddScreenObject(AddScreenObjectEvent e) {
            ScreenObjects.Add(e.ScreenObject);
        }

        private void OnRemoveScreenObject(RemoveScreenObjectEvent e) {
            ScreenObjects.Remove(e.ScreenObject);
        }
    }
}
