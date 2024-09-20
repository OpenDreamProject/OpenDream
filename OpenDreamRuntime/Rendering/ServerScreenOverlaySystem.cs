using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using Robust.Server.GameStates;

namespace OpenDreamRuntime.Rendering;

public sealed class ServerScreenOverlaySystem : SharedScreenOverlaySystem {
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    public void AddScreenObject(DreamConnection connection, DreamObjectMovable screenObject) {
        _pvsOverride.AddForceSend(screenObject.Entity, connection.Session);

        NetEntity ent = _entityManager.GetNetEntity(screenObject.Entity);
        RaiseNetworkEvent(new AddScreenObjectEvent(ent), connection.Session.Channel);
    }

    public void RemoveScreenObject(DreamConnection connection, DreamObjectMovable screenObject) {
        _pvsOverride.RemoveForceSend(screenObject.Entity, connection.Session);

        NetEntity ent = _entityManager.GetNetEntity(screenObject.Entity);
        RaiseNetworkEvent(new RemoveScreenObjectEvent(ent), connection.Session.Channel);
    }
}
