using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;

namespace OpenDreamClient.Rendering {
    class ClientScreenOverlaySystem : SharedScreenOverlaySystem {
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
            //TODO: PVS currently gets in the way of rendering screen objects that are not nearby on the map
            foreach (EntityUid uid in ScreenObjects) {
                if (_entityManager.TryGetComponent(uid, out DMISpriteComponent sprite)) {
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
