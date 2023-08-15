using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using OpenDreamShared.Dream;
using Robust.Server.GameStates;
using Robust.Server.Player;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Rendering;
public sealed class ServerClientImagesSystem : SharedClientImagesSystem {
    [Dependency] private readonly ServerAppearanceSystem _serverAppearanceSystem = default!;
    [Dependency] private readonly AtomManager _atomManager = default!;

    public void AddImageObject(DreamConnection connection, DreamObjectImage imageObject) {
        DreamObject? loc = imageObject.GetAttachedLoc();
        if(loc == null)
            return;

        EntityUid locEntity = EntityUid.Invalid;
        uint locAppearanceID = 0;

        uint imageAppearanceID = _serverAppearanceSystem.AddAppearance(imageObject.Appearance!);

        if(loc is DreamObjectMovable movable)
            locEntity = movable.Entity;

        _atomManager.TryCreateAppearanceFrom(new DreamValue(loc), out IconAppearance? locAppearance);
        if(locAppearance == null)
            return; //this could only happen if loc was invalid, so that's fine
        _atomManager.UpdateAppearance(loc, appearance => {appearance.ClientImages.Add(imageAppearanceID);});
        if(!_atomManager.TryGetAppearance(loc, out locAppearance)) //get the updated appearance
            return;

        locAppearanceID = _serverAppearanceSystem.AddAppearance(locAppearance);

        RaiseNetworkEvent(new AddClientImageEvent(locEntity, locAppearanceID, imageAppearanceID), connection.Session.ConnectedClient);
    }

    public void RemoveImageObject(DreamConnection connection, DreamObjectImage imageObject) {
        DreamObject? loc = imageObject.GetAttachedLoc();
        if(loc == null)
            return;

        EntityUid locEntity = EntityUid.Invalid;
        uint locAppearanceID = 0;

        uint imageAppearanceID = _serverAppearanceSystem.AddAppearance(imageObject.Appearance!);

        if(loc is DreamObjectMovable)
            locEntity = ((DreamObjectMovable)loc).Entity;

        _atomManager.TryCreateAppearanceFrom(new DreamValue(loc), out IconAppearance? locAppearance);
        if(locAppearance == null)
            return; //this could only happen if loc was invalid, so that's fine
        _atomManager.UpdateAppearance(loc, appearance => {appearance.ClientImages.Remove(imageAppearanceID);});
        if(!_atomManager.TryGetAppearance(loc, out locAppearance)) //get the updated appearance
            return;
        locAppearanceID = _serverAppearanceSystem.AddAppearance(locAppearance);

        _atomManager.TryCreateAppearanceFrom(new DreamValue(loc), out IconAppearance? attachedAppearance);
        if(attachedAppearance == null)
            return;

        RaiseNetworkEvent(new RemoveClientImageEvent(locEntity, locAppearanceID, imageAppearanceID), connection.Session.ConnectedClient);
    }
}
