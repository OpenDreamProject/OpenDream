using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using OpenDreamShared.Dream;
using Robust.Server.GameStates;
using Robust.Server.Player;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Rendering {
    public sealed class ServerClientImagesSystem : SharedClientImagesSystem {
        [Dependency] private readonly ServerAppearanceSystem serverAppearanceSystem = default!;
        [Dependency] private readonly IAtomManager atomManager = default!;

        public override void Initialize() {
            SubscribeLocalEvent<ExpandPvsEvent>(HandleExpandPvsEvent);
        }

        public void AddImageObject(DreamConnection connection, DreamObjectImage imageObject) {

            DreamObject? loc = imageObject.GetAttachedLoc();
            if(loc == null)
                return;

            EntityUid locEntity = EntityUid.Invalid;
            uint locAppearanceID = 0;

            uint imageAppearanceID = serverAppearanceSystem.AddAppearance(imageObject.Appearance!);

            if(loc is DreamObjectMovable)
                locEntity = ((DreamObjectMovable)loc).Entity;

            atomManager.TryCreateAppearanceFrom(new DreamValue(loc), out IconAppearance? locAppearance);
            if(locAppearance == null)
                return; //this could only happen if loc was invalid, so that's fine
            atomManager.UpdateAppearance(loc, appearance => {appearance.ClientImages.Add(imageAppearanceID);});
            if(!atomManager.TryGetAppearance(loc, out locAppearance)) //get the updated appearance
                return;
            if(!serverAppearanceSystem.TryGetAppearanceId(locAppearance, out locAppearanceID))
                locAppearanceID = serverAppearanceSystem.AddAppearance(locAppearance);

            RaiseNetworkEvent(new AddClientImageEvent(locEntity, locAppearanceID, imageAppearanceID), connection.Session.ConnectedClient);
        }

        public void RemoveImageObject(DreamConnection connection, DreamObjectImage imageObject) {
            DreamObject? loc = imageObject.GetAttachedLoc();
            if(loc == null)
                return;

            EntityUid locEntity = EntityUid.Invalid;
            uint locAppearanceID = 0;

            uint imageAppearanceID = serverAppearanceSystem.AddAppearance(imageObject.Appearance!);

            if(loc is DreamObjectMovable)
                locEntity = ((DreamObjectMovable)loc).Entity;

            atomManager.TryCreateAppearanceFrom(new DreamValue(loc), out IconAppearance? locAppearance);
            if(locAppearance == null)
                return; //this could only happen if loc was invalid, so that's fine
            atomManager.UpdateAppearance(loc, appearance => {appearance.ClientImages.Remove(imageAppearanceID);});
            if(!atomManager.TryGetAppearance(loc, out locAppearance)) //get the updated appearance
                return;
            if(!serverAppearanceSystem.TryGetAppearanceId(locAppearance, out locAppearanceID))
                locAppearanceID = serverAppearanceSystem.AddAppearance(locAppearance);

            atomManager.TryCreateAppearanceFrom(new DreamValue(loc), out IconAppearance? attachedAppearance);
            if(attachedAppearance == null)
                return;

            RaiseNetworkEvent(new RemoveClientImageEvent(locEntity, locAppearanceID, imageAppearanceID), connection.Session.ConnectedClient);
        }

        private void HandleExpandPvsEvent(ref ExpandPvsEvent e) {
            // what is this for?
        }
    }
}
