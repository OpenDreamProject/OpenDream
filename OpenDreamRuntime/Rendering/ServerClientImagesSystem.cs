﻿using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using OpenDreamShared.Dream;
using Robust.Server.GameStates;
using Robust.Server.Player;
using OpenDreamRuntime.Objects;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamRuntime.Rendering;
public sealed class ServerClientImagesSystem : SharedClientImagesSystem {
    [Dependency] private readonly ServerAppearanceSystem _serverAppearanceSystem = default!;
    [Dependency] private readonly AtomManager _atomManager = default!;

    public void AddImageObject(DreamConnection connection, DreamObjectImage imageObject) {
        DreamObject? loc = imageObject.GetAttachedLoc();
        if(loc == null)
            return;

        EntityUid locEntity = EntityUid.Invalid;
        Vector3 turfCoords = Vector3.Zero;
        uint locAppearanceID = 0;

        uint imageAppearanceID = _serverAppearanceSystem.AddAppearance(imageObject.Appearance!);

        if(loc is DreamObjectMovable movable)
            locEntity = movable.Entity;
        else if(loc is DreamObjectTurf turf)
            turfCoords = new Vector3(turf.X, turf.Y, turf.Z);

        RaiseNetworkEvent(new AddClientImageEvent(locEntity, turfCoords, imageAppearanceID), connection.Session.ConnectedClient);
    }

    public void RemoveImageObject(DreamConnection connection, DreamObjectImage imageObject) {
        DreamObject? loc = imageObject.GetAttachedLoc();
        if(loc == null)
            return;

        EntityUid locEntity = EntityUid.Invalid;
        Vector3 turfCoords = Vector3.Zero;

        uint imageAppearanceID = _serverAppearanceSystem.AddAppearance(imageObject.Appearance!);

        if(loc is DreamObjectMovable)
            locEntity = ((DreamObjectMovable)loc).Entity;
        else if(loc is DreamObjectTurf turf)
            turfCoords = new Vector3(turf.X, turf.Y, turf.Z);


        RaiseNetworkEvent(new RemoveClientImageEvent(locEntity, turfCoords, imageAppearanceID), connection.Session.ConnectedClient);
    }
}
