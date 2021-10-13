using OpenDreamRuntime.Objects;
using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace OpenDreamRuntime.Rendering {
    class ServerScreenOverlaySystem : SharedScreenOverlaySystem {
        [Dependency] IAtomManager _atomManager = default!;

        public void AddScreenObject(DreamConnection connection, DreamObject screenObject) {
            EntityUid entityId = _atomManager.GetAtomEntity(screenObject).Uid;

            RaiseNetworkEvent(new AddScreenObjectEvent(entityId), connection.Session.ConnectedClient);
        }

        public void RemoveScreenObject(DreamConnection connection, DreamObject screenObject) {
            EntityUid entityId = _atomManager.GetAtomEntity(screenObject).Uid;

            RaiseNetworkEvent(new RemoveScreenObjectEvent(entityId), connection.Session.ConnectedClient);
        }
    }
}
