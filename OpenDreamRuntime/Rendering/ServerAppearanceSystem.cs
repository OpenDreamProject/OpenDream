using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Network;
using System.Diagnostics;
using System.Linq;

namespace OpenDreamRuntime.Rendering;

public sealed class ServerAppearanceSystem : SharedAppearanceSystem {
    /// <summary>
    /// Each appearance gets a unique ID when marked as registered. Here we store these as a key -> weakref in a weaktable, which does not count
    /// as a hard ref but allows quick lookup. Each object which holds an appearance MUST hold that ImmutableAppearance until it is no longer
    /// needed or it will be GC'd. Overlays & underlays are stored as hard refs on the ImmutableAppearance so you only need to hold the main appearance.
    /// </summary>
    private readonly HashSet<ProxyWeakRef> _appearanceLookup = new();
    private readonly Dictionary<uint, ProxyWeakRef> _idToAppearance = new();
    private uint _counter;
    private readonly LinkedList<AppearanceQueueNode> _ttlQueue = new();
    private readonly Dictionary<ImmutableAppearance, LinkedListNode<AppearanceQueueNode>> _ttlQueueHashtable = new();
    private readonly double _ttl = 60; //seconds
    public readonly ImmutableAppearance DefaultAppearance;
    [Dependency] private readonly IServerNetManager _networkManager = default!;

    /// <summary>
    /// This system is used by the PVS thread, we need to be thread-safe
    /// </summary>
    private readonly object _lock = new();

    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public ServerAppearanceSystem() {
        DefaultAppearance = new ImmutableAppearance(MutableAppearance.Default, this);
        DefaultAppearance.MarkRegistered(_counter++); //first appearance registered gets id 0, this is the blank default appearance
        ProxyWeakRef proxyWeakRef = new(DefaultAppearance);
        _appearanceLookup.Add(proxyWeakRef);
        _idToAppearance.Add(DefaultAppearance.MustGetID(), proxyWeakRef);
        //leaving this in as a sanity check for mutable and immutable appearance hashcodes covering all the same vars
        //if this debug assert fails, you've probably changed appearance var and not updated its counterpart
        Debug.Assert(DefaultAppearance.GetHashCode() == MutableAppearance.Default.GetHashCode());
    }

    public override void Initialize() {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown() {
        lock (_lock) {
            _appearanceLookup.Clear();
            _idToAppearance.Clear();
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
        if (e.NewStatus == SessionStatus.InGame) {
            //todo this is probably stupid slow
            lock (_lock) {
                Dictionary<uint, ImmutableAppearance> sendData = new(_appearanceLookup.Count);

                foreach(ProxyWeakRef proxyWeakRef in _appearanceLookup){
                    if(proxyWeakRef.TryGetTarget(out var immutable))
                        sendData.Add(immutable.MustGetID(), immutable);
                }

                e.Session.Channel.SendMessage(new MsgAllAppearances(sendData));
            }

        }
    }

    private void RegisterAppearance(ImmutableAppearance immutableAppearance) {
        immutableAppearance.MarkRegistered(_counter++); //lets this appearance know it needs to do GC finaliser & get an ID
        ProxyWeakRef proxyWeakRef = new(immutableAppearance);
        _appearanceLookup.Add(proxyWeakRef);
        _idToAppearance.Add(immutableAppearance.MustGetID(), proxyWeakRef);
        _networkManager.ServerSendToAll(new MsgNewAppearance(immutableAppearance));
    }

    public ImmutableAppearance AddAppearance(MutableAppearance appearance, bool registerApearance = true) {
        ImmutableAppearance immutableAppearance = new(appearance, this);

        lock (_lock) {
            while(_ttlQueue.First is not null && _ttlQueue.First.Value.Expiry < DateTime.Now) {
                _ttlQueueHashtable.Remove(_ttlQueue.First.Value.Appearance);
                _ttlQueue.RemoveFirst();
            }

            if(_appearanceLookup.TryGetValue(new(immutableAppearance), out var weakReference) && weakReference.TryGetTarget(out var originalImmutable)) {
                if(_ttlQueueHashtable.TryGetValue(originalImmutable, out var linkedListNode)) { //if we already got it, reset its position in the queue
                    linkedListNode.ValueRef.Expiry = DateTime.Now.AddSeconds(_ttl);
                    _ttlQueue.Remove(linkedListNode);
                    _ttlQueue.AddLast(linkedListNode);
                } else { //else add it to the queue
                    _ttlQueueHashtable.Add(originalImmutable, _ttlQueue.AddLast(new AppearanceQueueNode(originalImmutable, DateTime.Now.AddSeconds(_ttl))));
                }

                return originalImmutable;
            } else if (registerApearance) {
                RegisterAppearance(immutableAppearance);
                //we absolutely should not already have it, so just add it to the queue
                _ttlQueueHashtable.Add(immutableAppearance, _ttlQueue.AddLast(new AppearanceQueueNode(immutableAppearance, DateTime.Now.AddSeconds(_ttl))));
                return immutableAppearance;
            } else {
                //don't bother for unregistered
                return immutableAppearance;
            }
        }
    }

    //this should only be called by the ImmutableAppearance's finalizer
    public override void RemoveAppearance(ImmutableAppearance appearance) {
        lock (_lock) {
            ProxyWeakRef proxyWeakRef = new(appearance);
            if(_appearanceLookup.TryGetValue(proxyWeakRef, out var weakRef)) {
                //it is possible that a new appearance was created with the same hash before the GC got around to cleaning up the old one
                if(weakRef.TryGetTarget(out var target) && !ReferenceEquals(target,appearance))
                    return;
                _appearanceLookup.Remove(proxyWeakRef);
                RaiseNetworkEvent(new RemoveAppearanceEvent(appearance.MustGetID()));
            }
        }
    }

    public override ImmutableAppearance MustGetAppearanceById(uint appearanceId) {
        lock (_lock) {
            if(!_idToAppearance[appearanceId].TryGetTarget(out var result))
                throw new Exception($"Attempted to access deleted appearance ID ${appearanceId} in MustGetAppearanceByID()");
            return result;
        }
    }

    public bool TryGetAppearanceById(uint appearanceId, [NotNullWhen(true)] out ImmutableAppearance? appearance) {
        lock (_lock) {
            appearance = null;
            return _idToAppearance.TryGetValue(appearanceId, out var appearanceRef) && appearanceRef.TryGetTarget(out appearance);
        }
    }

    public void Animate(NetEntity entity, MutableAppearance targetAppearance, TimeSpan duration, AnimationEasing easing, int loop, AnimationFlags flags, int delay, bool chainAnim, uint? turfId) {
        uint appearanceId = AddAppearance(targetAppearance).MustGetID();

        RaiseNetworkEvent(new AnimationEvent(entity, appearanceId, duration, easing, loop, flags, delay, chainAnim, turfId));
    }
}

struct AppearanceQueueNode {
    public ImmutableAppearance Appearance;
    public DateTime Expiry;

    public AppearanceQueueNode(ImmutableAppearance appearance, DateTime expiry) {
        Appearance = appearance;
        Expiry = expiry;
    }
}

//this class lets us hold a weakref and also do quick lookups in hash tables
internal sealed class ProxyWeakRef(ImmutableAppearance appearance) : IEquatable<ProxyWeakRef>{
    public WeakReference<ImmutableAppearance> WeakRef = new(appearance);
    public bool IsAlive => WeakRef.TryGetTarget(out var _);
    public bool TryGetTarget([NotNullWhen(true)] out ImmutableAppearance? target) => WeakRef.TryGetTarget(out target);

    public override int GetHashCode() {
        if(WeakRef.TryGetTarget(out var target))
            return target.GetHashCode();
        return 0; //equals will catch this in the off chance that this causes a collision
    }

    public override bool Equals(object? obj) => obj is ProxyWeakRef proxy && Equals(proxy) || obj is ImmutableAppearance immutable && Equals(immutable);

    public bool Equals(ProxyWeakRef? proxy) {
        if(proxy is not null && WeakRef.TryGetTarget(out ImmutableAppearance? thisRef) && proxy.WeakRef.TryGetTarget(out ImmutableAppearance? thatRef))
            return thisRef.Equals(thatRef);
        return false;
    }

    public bool Equals(ImmutableAppearance? immutable){
        if(WeakRef.TryGetTarget(out ImmutableAppearance? thisRef))
            return thisRef.Equals(immutable);
        return false;
    }
}
