using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;

namespace OpenDreamRuntime.Rendering {
    public sealed class ServerAppearanceSystem : SharedAppearanceSystem {
        private readonly Dictionary<IconAppearance, int> _appearanceToId = new();
        private readonly Dictionary<int, IconAppearance> _idToAppearance = new();
        private int _appearanceIdCounter;

        /// <summary>
        /// This system is used by the PVS thread, we need to be thread-safe
        /// </summary>
        private readonly object _lock = new();

        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize() {
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
                RaiseNetworkEvent(new AllAppearancesEvent(_idToAppearance), e.Session.ConnectedClient);
            }
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

        public void Animate(NetEntity entity, IconAppearance targetAppearance, TimeSpan duration) {
            int appearanceId = AddAppearance(targetAppearance);

            RaiseNetworkEvent(new AnimationEvent(entity, appearanceId, duration));
        }
    }
}
