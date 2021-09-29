using System.Collections.Generic;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace OpenDreamRuntime {
    class AppearanceSystem : SharedAppearanceSystem {
        private Dictionary<IconAppearance, uint> _appearanceToId = new();
        private Dictionary<uint, IconAppearance> _idToAppearance = new();
        private uint _appearanceIdCounter = 0;

        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize() {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public override void Shutdown() {
            _appearanceToId.Clear();
            _appearanceIdCounter = 0;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
            if (e.NewStatus == SessionStatus.InGame) {
                Robust.Shared.Log.Logger.Debug($"Sending all");
                RaiseNetworkEvent(new AllAppearancesEvent(), e.Session.ConnectedClient);
            }
        }

        public uint AddAppearance(IconAppearance appearance) {
            if (!_appearanceToId.TryGetValue(appearance, out uint appearanceId)) {
                appearanceId = _appearanceIdCounter++;
                _appearanceToId.Add(appearance, appearanceId);
                _idToAppearance.Add(appearanceId, appearance);
                Robust.Shared.Log.Logger.Debug($"Sending {appearanceId}");
                RaiseNetworkEvent(new NewAppearanceEvent());
            }

            return appearanceId;
        }
    }
}
