using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Network;
using System.Diagnostics;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Rendering;

public sealed class ServerAppearanceSystem : SharedAppearanceSystem {
    /// <summary>
    /// Each appearance's HashCode is used as its ID. Here we store these as weakrefs, so each object which holds an appearance MUST
    /// hold that ImmutableIconAppearance until it is no longer needed. Overlays & underlays are stored as hard refs on the ImmutableIconAppearance
    /// so you only need to hold the main appearance.
    /// </summary>
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
                Dictionary<int, ImmutableIconAppearance> sendData = new(_idToAppearance.Count);

                foreach(int key in _idToAppearance.Keys){
                    if(_idToAppearance[key].TryGetTarget(out var immutable))
                        sendData.Add(key, immutable);
                }

                e.Session.Channel.SendMessage(new MsgAllAppearances(sendData));
            }

        }
    }

    public ImmutableIconAppearance AddAppearance(MutableIconAppearance appearance, bool registerApearance = true) {
        ImmutableIconAppearance immutableAppearance = new(appearance, this);
        //if this debug assert fails, you've probably changed an icon appearance var and not updated its counterpart
        //this debug MUST pass. A number of things rely on these hashcodes being equivalent *on the server*.
        DebugTools.Assert(appearance.GetHashCode() == immutableAppearance.GetHashCode());
        lock (_lock) {
            if(_idToAppearance.TryGetValue(immutableAppearance.GetHashCode(), out var weakReference) && weakReference.TryGetTarget(out var originalImmutable)) {
                return originalImmutable;
            } else if (registerApearance) {
                immutableAppearance.MarkRegistered(); //lets this appearance know it needs to do GC finaliser
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
            }
        }
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
