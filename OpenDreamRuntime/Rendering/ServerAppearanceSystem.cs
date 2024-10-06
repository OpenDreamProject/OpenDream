using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Player;

namespace OpenDreamRuntime.Rendering;

public sealed class ServerAppearanceSystem : SharedAppearanceSystem {
    private readonly Dictionary<ImmutableIconAppearance, int> _appearanceToId = new();
    private readonly Dictionary<int, ImmutableIconAppearance> _idToAppearance = new();
    private readonly Dictionary<ImmutableIconAppearance, int> _appearanceRefCounts = new();
    private int _appearanceIdCounter;


    /// <summary>
    /// This system is used by the PVS thread, we need to be thread-safe
    /// </summary>
    private readonly object _lock = new();

    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize() {
        //register empty appearance as ID 0
        _appearanceToId.Add(ImmutableIconAppearance.Default, 0);
        _idToAppearance.Add(0, ImmutableIconAppearance.Default);
        _appearanceRefCounts.Add(ImmutableIconAppearance.Default, 1);
        _appearanceIdCounter = 1;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
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
            e.Session.Channel.SendMessage(new MsgAllAppearances(_idToAppearance));
        }
    }

    public int AddAppearance(IconAppearance appearance) {
        ImmutableIconAppearance immutableAppearance = new(appearance);
        lock (_lock) {
            if (!_appearanceToId.TryGetValue(immutableAppearance, out int appearanceId)) {
                appearanceId = _appearanceIdCounter++;
                _appearanceToId.Add(immutableAppearance, appearanceId);
                _idToAppearance.Add(appearanceId, immutableAppearance);
                RaiseNetworkEvent(new NewAppearanceEvent(appearanceId, immutableAppearance));
            }
            return appearanceId;
        }
    }

    public ImmutableIconAppearance MustGetAppearance(int appearanceId) {
        lock (_lock) {
            return _idToAppearance[appearanceId];
        }
    }

    public bool TryGetAppearance(int appearanceId, [NotNullWhen(true)] out ImmutableIconAppearance? appearance) {
        lock (_lock) {
            return _idToAppearance.TryGetValue(appearanceId, out appearance);
        }
    }

    public void Animate(NetEntity entity, IconAppearance targetAppearance, TimeSpan duration, AnimationEasing easing, int loop, AnimationFlags flags, int delay, bool chainAnim) {
        int appearanceId = AddAppearance(targetAppearance);

        RaiseNetworkEvent(new AnimationEvent(entity, appearanceId, duration, easing, loop, flags, delay, chainAnim));
    }
}
