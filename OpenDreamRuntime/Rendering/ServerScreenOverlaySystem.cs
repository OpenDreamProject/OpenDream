using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using Robust.Server.GameStates;

namespace OpenDreamRuntime.Rendering;

public sealed partial class ServerScreenOverlaySystem : SharedScreenOverlaySystem {
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;

    public void AddScreenObject(DreamConnection connection, DreamObjectMovable screenObject) {
        if (connection.Session is null)
            return;

        _pvsOverride.AddForceSend(screenObject.Entity, connection.Session);

        NetEntity ent = _entityManager.GetNetEntity(screenObject.Entity);
        RaiseNetworkEvent(new AddScreenObjectEvent(ent), connection.Session.Channel);
    }

    public void RemoveScreenObject(DreamConnection connection, DreamObjectMovable screenObject) {
        if (connection.Session is null)
            return;

        _pvsOverride.RemoveForceSend(screenObject.Entity, connection.Session);

        NetEntity ent = _entityManager.GetNetEntity(screenObject.Entity);
        RaiseNetworkEvent(new RemoveScreenObjectEvent(ent), connection.Session.Channel);
    }
}
