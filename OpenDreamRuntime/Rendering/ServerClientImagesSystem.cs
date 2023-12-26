using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using OpenDreamRuntime.Objects;
using Vector3 = Robust.Shared.Maths.Vector3;
using Robust.Server.GameStates;

namespace OpenDreamRuntime.Rendering;
public sealed class ServerClientImagesSystem : SharedClientImagesSystem {

    [Dependency] private readonly PvsOverrideSystem _pvsOverrideSystem = default!;
    public void AddImageObject(DreamConnection connection, DreamObjectImage imageObject) {
        DreamObject? loc = imageObject.GetAttachedLoc();
        if(loc == null)
            return;

        EntityUid locEntity = EntityUid.Invalid;
        Vector3 turfCoords = Vector3.Zero;

        if(loc is DreamObjectMovable movable)
            locEntity = movable.Entity;
        else if(loc is DreamObjectTurf turf)
            turfCoords = new Vector3(turf.X, turf.Y, turf.Z);

        NetEntity ent = GetNetEntity(locEntity);
        EntityUid imageObjectEntity = imageObject.GetEntity();
        NetEntity imageObjectNetEntity = GetNetEntity(imageObjectEntity);
        if (imageObjectNetEntity != NetEntity.Invalid)
            _pvsOverrideSystem.AddGlobalOverride(imageObjectNetEntity);
        RaiseNetworkEvent(new AddClientImageEvent(ent, turfCoords, imageObjectNetEntity), connection.Session!.Channel);
    }

    public void RemoveImageObject(DreamConnection connection, DreamObjectImage imageObject) {
        DreamObject? loc = imageObject.GetAttachedLoc();
        if(loc == null)
            return;

        EntityUid locEntity = EntityUid.Invalid;
        Vector3 turfCoords = Vector3.Zero;

        if(loc is DreamObjectMovable)
            locEntity = ((DreamObjectMovable)loc).Entity;
        else if(loc is DreamObjectTurf turf)
            turfCoords = new Vector3(turf.X, turf.Y, turf.Z);


        NetEntity ent = GetNetEntity(locEntity);
        NetEntity imageObjectNetEntity = GetNetEntity(imageObject.GetEntity());
        RaiseNetworkEvent(new RemoveClientImageEvent(ent, turfCoords, imageObjectNetEntity), connection.Session!.Channel);
    }
}
