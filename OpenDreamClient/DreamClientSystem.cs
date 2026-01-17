using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Network.Messages;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace OpenDreamClient;

internal sealed class DreamClientSystem : EntitySystem {
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    // Current NetEntityof player's mob, or Invalid if could not be determined.
    private NetEntity _mobNet = NetEntity.Invalid;

    // Current Entity of player's mob, or Invalid if could not be determined.
    private EntityUid _mobUid = EntityUid.Invalid;

    // Sometimes we get mob info before we know all the net entities, so store the net entity and refer to it 
    public EntityUid MobUid {
        get {
            // if entity of mob is invalid but net entity isn't, try referring to known net entities
            if (! _mobUid.IsValid() && _mobNet.IsValid() && _entityManager.TryGetEntity(_mobNet, out var ent)) {
                _mobUid = ent.GetValueOrDefault(EntityUid.Invalid);
            }

            return _mobUid;
        }
    }

    // Current Entity of player's eye, or Invalid if could not be determined.
    private ClientObjectReference _eyeRef = new(NetEntity.Invalid);

    public ClientObjectReference EyeRef {
        get {
            if (_eyeRef.Type == ClientObjectReference.RefType.Entity && !_eyeRef.Entity.IsValid()) {
                return new(_entityManager.GetNetEntity(MobUid));
            } else {
                return _eyeRef;
            }
        }
        private set {
            _eyeManager.CurrentEye = new DreamClientEye(_eyeManager.CurrentEye, value, _entityManager, _transformSystem);
            _eyeRef = value;
        }
    }

    public bool IsEyeMissing() {
        return EyeRef.Type == ClientObjectReference.RefType.Client;
    }

    public override void Initialize() {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent e) {
        // The active input context gets reset to "common" when a new player is attached
        // So we have to set it again
        _interfaceManager.DefaultWindow?.Macro.SetActive();
    }

    public void RxNotifyMobEyeUpdate(MsgNotifyMobEyeUpdate msg) {
        var prevMobNet = _mobNet;
        _mobNet = msg.MobNetEntity;

        if (prevMobNet != _mobNet) {
            // mark mob cache as dirty/invalid
            _mobUid = EntityUid.Invalid;
        }

        var incomingEyeRef = msg.EyeRef;

        if (incomingEyeRef.Type == ClientObjectReference.RefType.Entity && !incomingEyeRef.Entity.IsValid()) {
            EyeRef = new(msg.MobNetEntity);
        } else {
            EyeRef = incomingEyeRef;
        }
    }
}
