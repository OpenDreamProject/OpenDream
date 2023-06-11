﻿using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using System.Diagnostics.CodeAnalysis;

namespace OpenDreamRuntime.Rendering {
    public sealed class ServerAppearanceSystem : SharedAppearanceSystem {
        private readonly Dictionary<IconAppearance, uint> _appearanceToId = new();
        private readonly Dictionary<uint, IconAppearance> _idToAppearance = new();
        private uint _appearanceIdCounter;

        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize() {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public override void Shutdown() {
            _appearanceToId.Clear();
            _idToAppearance.Clear();
            _appearanceIdCounter = 0;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
            if (e.NewStatus == SessionStatus.InGame) {
                RaiseNetworkEvent(new AllAppearancesEvent(_idToAppearance), e.Session.ConnectedClient);
            }
        }

        public uint AddAppearance(IconAppearance appearance) {
            if (!_appearanceToId.TryGetValue(appearance, out uint appearanceId)) {
                appearanceId = _appearanceIdCounter++;
                _appearanceToId.Add(appearance, appearanceId);
                _idToAppearance.Add(appearanceId, appearance);
                RaiseNetworkEvent(new NewAppearanceEvent(appearanceId, appearance));
            }

            return appearanceId;
        }

        public IconAppearance MustGetAppearance(uint appearanceId) {
            return _idToAppearance[appearanceId];
        }

        public bool TryGetAppearance(uint appearanceId, [NotNullWhen(true)] out IconAppearance? appearance) {
            return _idToAppearance.TryGetValue(appearanceId, out appearance);
        }

        public void Animate(EntityUid entity, IconAppearance targetAppearance, TimeSpan duration) {
            uint appearanceId = AddAppearance(targetAppearance);

            RaiseNetworkEvent(new AnimationEvent(entity, appearanceId, duration));
        }
    }
}
