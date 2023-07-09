using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using Robust.Server.GameStates;
using Robust.Server.Player;

namespace OpenDreamRuntime.Rendering {
    public sealed class ServerClientImagesSystem : SharedClientImagesSystem {
        private readonly Dictionary<IPlayerSession, Dictionary<EntityUid, List<DreamObjectImage>>> _sessionToObjectImageDict = new();
        [Dependency] private readonly ServerAppearanceSystem serverAppearanceSystem = default!;

        public override void Initialize() {
            SubscribeLocalEvent<ExpandPvsEvent>(HandleExpandPvsEvent);
        }

        public void AddImageObject(DreamConnection connection, DreamObjectImage imageObject) {
            if (!_sessionToObjectImageDict.TryGetValue(connection.Session, out var objectToImages)) {
                objectToImages = new Dictionary<EntityUid, List<DreamObjectImage>>();
                _sessionToObjectImageDict.Add(connection.Session, objectToImages);
            }
            EntityUid? attachedEntity = imageObject.GetAttachedEntity();
            if(attachedEntity == null)
                return;
            uint appID = serverAppearanceSystem.AddAppearance(imageObject.Appearance);
            if(!objectToImages.TryGetValue(attachedEntity.Value, out var imageList))
                imageList = new List<DreamObjectImage>();
            imageList.Add(imageObject);
            objectToImages[attachedEntity.Value] = imageList;

            RaiseNetworkEvent(new AddClientImageEvent(attachedEntity.Value, appID), connection.Session.ConnectedClient);
        }

        public void RemoveImageObject(DreamConnection connection, DreamObjectImage imageObject) {
            EntityUid? attachedEntity = imageObject.GetAttachedEntity();
            if(attachedEntity == null)
                return;

            if(serverAppearanceSystem.TryGetAppearanceId(imageObject.Appearance, out uint appID)){
                var objectToImages = _sessionToObjectImageDict[connection.Session];
                objectToImages[attachedEntity.Value].Remove(imageObject);
                RaiseNetworkEvent(new RemoveClientImageEvent(attachedEntity.Value, appID), connection.Session.ConnectedClient);
            }
        }

        private void HandleExpandPvsEvent(ref ExpandPvsEvent e) {
            if (_sessionToObjectImageDict.TryGetValue(e.Session, out var objects)) {
                e.Entities.AddRange(objects.Keys);
            }
        }
    }
}
