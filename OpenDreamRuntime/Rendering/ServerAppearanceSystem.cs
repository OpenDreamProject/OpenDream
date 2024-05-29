using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;

namespace OpenDreamRuntime.Rendering;

public sealed class ServerAppearanceSystem : SharedAppearanceSystem {
    private readonly Dictionary<IconAppearance, int> _appearanceToId = new();
    private readonly Dictionary<int, IconAppearance> _idToAppearance = new();
    private readonly Dictionary<IconAppearance, int> _appearanceRefCounts = new();
    private int _appearanceIdCounter;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// This system is used by the PVS thread, we need to be thread-safe
    /// </summary>
    private readonly object _lock = new();

    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize() {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _sawmill ??= Logger.GetSawmill("ServerAppearanceSystem");
    }

    public override void Shutdown() {
        lock (_lock) {
            _appearanceToId.Clear();
            _idToAppearance.Clear();
            _appearanceIdCounter = 0;
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
        if (e.NewStatus == SessionStatus.InGame) {
            _sawmill.Debug($"Sending {_idToAppearance.Count} appearances to {e.Session.Channel.UserName}");
            RaiseNetworkEvent(new AllAppearancesEvent(_idToAppearance), e.Session.Channel);
        }
    }

    public void IncreaseAppearanceRefCount(IconAppearance appearance) {
        lock (_lock) {
            int count = _appearanceRefCounts.GetValueOrDefault(appearance, 0);
            foreach(var overlayid in appearance.Overlays) {
                IncreaseAppearanceRefCount(overlayid);
            }
            foreach(var underlayid in appearance.Underlays) {
                IncreaseAppearanceRefCount(underlayid);
            }
            _appearanceRefCounts[appearance] = count + 1;
        }
    }
    public void IncreaseAppearanceRefCount(int appearanceId) {
        if (!_idToAppearance.TryGetValue(appearanceId, out IconAppearance? appearance)) {
            throw new InvalidOperationException("Trying to increase ref count of an appearance that doesn't exist.");
        }

        IncreaseAppearanceRefCount(appearance);
    }

    public void DecreaseAppearanceRefCount(IconAppearance appearance) {
        lock (_lock) {
            if (!_appearanceRefCounts.TryGetValue(appearance, out int count)) {
                throw new InvalidOperationException($"Appearance {appearance.GetHashCode()} ref count is already 0. You might be trying to remove an appearance that was never added.");
            }

            if (count == 1) {
                foreach(var overlayid in appearance.Overlays) {
                    DecreaseAppearanceRefCount(overlayid);
                }
                foreach(var underlayid in appearance.Underlays) {
                    DecreaseAppearanceRefCount(underlayid);
                }
                if(_appearanceToId.TryGetValue(appearance, out int id)) {
                    _idToAppearance.Remove(id);
                    RaiseNetworkEvent(new RemoveAppearanceEvent(id));
                }
                _appearanceRefCounts.Remove(appearance);
                _appearanceToId.Remove(appearance);
                //let the GC sort out the rest
            } else {
                _appearanceRefCounts[appearance] = count - 1;
            }
        }
    }
    public void DecreaseAppearanceRefCount(int appearanceId) {
        if (!_idToAppearance.TryGetValue(appearanceId, out IconAppearance? appearance)) {
            throw new InvalidOperationException("Trying to decrease ref count of an appearance that doesn't exist.");
        }

        DecreaseAppearanceRefCount(appearance);
    }
    public int AddAppearance(IconAppearance appearance) {
        lock (_lock) {
            if (!_appearanceToId.TryGetValue(appearance, out int appearanceId)) {
                appearanceId = _appearanceIdCounter++;
                _appearanceToId.Add(appearance, appearanceId);
                _idToAppearance.Add(appearanceId, appearance);
                RaiseNetworkEvent(new NewAppearanceEvent(appearanceId, appearance));
            }
            return appearanceId;
        }
    }

    public IconAppearance MustGetAppearance(int appearanceId) {
        lock (_lock) {
            return _idToAppearance[appearanceId];
        }
    }

    public bool TryGetAppearance(int appearanceId, [NotNullWhen(true)] out IconAppearance? appearance) {
        lock (_lock) {
            return _idToAppearance.TryGetValue(appearanceId, out appearance);
        }
    }

    public bool TryGetAppearanceId(IconAppearance appearance, out int appearanceId) {
        lock (_lock) {
            return _appearanceToId.TryGetValue(appearance, out appearanceId);
        }
    }

    public void Animate(NetEntity entity, IconAppearance targetAppearance, TimeSpan duration, AnimationEasing easing, int loop, AnimationFlags flags, int delay, bool chainAnim) {
        int appearanceId = AddAppearance(targetAppearance);

        RaiseNetworkEvent(new AnimationEvent(entity, appearanceId, duration, easing, loop, flags, delay, chainAnim));
    }
}
