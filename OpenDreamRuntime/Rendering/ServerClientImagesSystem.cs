using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using OpenDreamRuntime.Objects;
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
        EntityUid imageObjectEntity = imageObject.Entity;
        NetEntity imageObjectNetEntity = GetNetEntity(imageObjectEntity);
        if (imageObjectEntity != EntityUid.Invalid)
            _pvsOverrideSystem.AddSessionOverride(imageObjectEntity, connection.Session!);
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
        EntityUid imageObjectEntity = imageObject.Entity;
        if (imageObjectEntity != EntityUid.Invalid)
            _pvsOverrideSystem.RemoveSessionOverride(imageObjectEntity, connection.Session!);
        NetEntity imageObjectNetEntity = GetNetEntity(imageObject.Entity);
        RaiseNetworkEvent(new RemoveClientImageEvent(ent, turfCoords, imageObjectNetEntity), connection.Session!.Channel);
    }
}
