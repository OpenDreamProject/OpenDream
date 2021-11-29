using System;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;

namespace OpenDreamClient {
    class ClientAppearanceSystem : SharedAppearanceSystem {
        private Dictionary<uint, IconAppearance> _appearances = new();
        private Dictionary<uint, List<Action<IconAppearance>>> _appearanceLoadCallbacks = new();

        public override void Initialize() {
            SubscribeNetworkEvent<AllAppearancesEvent>(OnAllAppearances);
            SubscribeNetworkEvent<NewAppearanceEvent>(OnNewAppearance);
        }

        public override void Shutdown() {
            _appearances.Clear();
            _appearanceLoadCallbacks.Clear();
        }

        public void LoadAppearance(uint appearanceId, Action<IconAppearance> loadCallback) {
            if (_appearances.TryGetValue(appearanceId, out IconAppearance appearance)) {
                loadCallback(appearance);
            }

            if (!_appearanceLoadCallbacks.ContainsKey(appearanceId)) {
                _appearanceLoadCallbacks.Add(appearanceId, new());
            }

            _appearanceLoadCallbacks[appearanceId].Add(loadCallback);
        }

        private void OnAllAppearances(AllAppearancesEvent e, EntitySessionEventArgs session) {
            _appearances = e.Appearances;

            foreach (KeyValuePair<uint, IconAppearance> pair in _appearances) {
                if (_appearanceLoadCallbacks.TryGetValue(pair.Key, out var callbacks)) {
                    foreach (var callback in callbacks) callback(pair.Value);
                }
            }
        }

        private void OnNewAppearance(NewAppearanceEvent e) {
            _appearances[e.AppearanceId] = e.Appearance;

            if (_appearanceLoadCallbacks.TryGetValue(e.AppearanceId, out var callbacks)) {
                foreach (var callback in callbacks) callback(e.Appearance);
            }
        }
    }
}
