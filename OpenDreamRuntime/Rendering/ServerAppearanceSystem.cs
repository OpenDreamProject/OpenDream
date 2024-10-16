using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using System.Diagnostics;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Rendering;

public sealed class ServerAppearanceSystem : SharedAppearanceSystem {
    //use the appearance hash as the id!
    //appearance hash to weakref
    private readonly Dictionary<int, WeakReference<ImmutableIconAppearance>> _idToAppearance = new();

    public readonly ImmutableIconAppearance DefaultAppearance;
    [Dependency] private readonly IServerNetManager _networkManager = default!;

    /// <summary>
    /// This system is used by the PVS thread, we need to be thread-safe
    /// </summary>
    private readonly object _lock = new();

    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public ServerAppearanceSystem() {
        DefaultAppearance = new ImmutableIconAppearance(MutableIconAppearance.Default, this);
        DefaultAppearance.MarkRegistered();
        Debug.Assert(DefaultAppearance.GetHashCode() == MutableIconAppearance.Default.GetHashCode());
    }

    public override void Initialize() {
        _idToAppearance.Add(DefaultAppearance.GetHashCode(), new(DefaultAppearance));
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown() {
        lock (_lock) {
            _idToAppearance.Clear();
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
        if (e.NewStatus == SessionStatus.InGame) {
            //todo this is probably stupid slow
            lock (_lock) {
                Dictionary<int, IBufferableAppearance> sendData = new(_idToAppearance.Count);

                foreach(int key in _idToAppearance.Keys){
                    ImmutableIconAppearance? immutable;
                    if(_idToAppearance[key].TryGetTarget(out immutable))
                        sendData.Add(key, immutable);
                }

                e.Session.Channel.SendMessage(new MsgAllAppearances(sendData));
            }

        }
    }

    public ImmutableIconAppearance AddAppearance(MutableIconAppearance appearance, bool RegisterApearance = true) {
        ImmutableIconAppearance immutableAppearance = new(appearance, this);
        DebugTools.Assert(appearance.GetHashCode() == immutableAppearance.GetHashCode());
        lock (_lock) {
            if(_idToAppearance.TryGetValue(immutableAppearance.GetHashCode(), out var weakReference) && weakReference.TryGetTarget(out var originalImmutable)) {
                return originalImmutable;
            } else if (RegisterApearance) {
                immutableAppearance.MarkRegistered();
                _idToAppearance[immutableAppearance.GetHashCode()] = new(immutableAppearance);
                _networkManager.ServerSendToAll(new MsgNewAppearance(immutableAppearance));
                return immutableAppearance;
            } else {
                return immutableAppearance;
            }
        }
    }

    //this should only be called by the ImmutableIconAppearance's finalizer
    public override void RemoveAppearance(ImmutableIconAppearance appearance) {
        lock (_lock) {
            if(_idToAppearance.TryGetValue(appearance.GetHashCode(), out var weakRef)) {
                //it is possible that a new appearance was created with the same hash before the GC got around to cleaning up the old one
                if(weakRef.TryGetTarget(out var target) && !ReferenceEquals(target,appearance))
                    return;
                _idToAppearance.Remove(appearance.GetHashCode());
                RaiseNetworkEvent(new RemoveAppearanceEvent(appearance.GetHashCode()));
}           }
    }

    public override ImmutableIconAppearance MustGetAppearanceById(int appearanceId) {
        lock (_lock) {
            if(!_idToAppearance[appearanceId].TryGetTarget(out var result))
                throw new Exception($"Attempted to access deleted appearance ID ${appearanceId} in MustGetAppearanceByID()");
            return result;
        }
    }

    public bool TryGetAppearanceById(int appearanceId, [NotNullWhen(true)] out ImmutableIconAppearance? appearance) {
        lock (_lock) {
            appearance = null;
            return _idToAppearance.TryGetValue(appearanceId, out var appearanceRef) && appearanceRef.TryGetTarget(out appearance);
        }
    }

    public void Animate(NetEntity entity, MutableIconAppearance targetAppearance, TimeSpan duration, AnimationEasing easing, int loop, AnimationFlags flags, int delay, bool chainAnim, int? turfId) {
        int appearanceId = AddAppearance(targetAppearance).GetHashCode();

        RaiseNetworkEvent(new AnimationEvent(entity, appearanceId, duration, easing, loop, flags, delay, chainAnim, turfId));
    }
}
