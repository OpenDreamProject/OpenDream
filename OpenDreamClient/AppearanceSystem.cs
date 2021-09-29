using System.Collections.Generic;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;

namespace OpenDreamClient {
    class AppearanceSystem : SharedAppearanceSystem {
        private Dictionary<uint, IconAppearance> _appearances = new();

        public override void Initialize() {
            SubscribeNetworkEvent<AllAppearancesEvent>(OnAllAppearances);
            SubscribeNetworkEvent<NewAppearanceEvent>(OnNewAppearance);
        }

        public override void Shutdown() {
            _appearances.Clear();
        }

        public bool TryGetAppearance(uint appearanceId, out IconAppearance appearance) {
            return _appearances.TryGetValue(appearanceId, out appearance);
        }

        private void OnAllAppearances(AllAppearancesEvent e, EntitySessionEventArgs session) {
            Robust.Shared.Log.Logger.Debug($"Received all appearances {e.Appearances.Count}");
            _appearances = e.Appearances;
        }

        private void OnNewAppearance(NewAppearanceEvent e) {
            Robust.Shared.Log.Logger.Debug($"Received {e.AppearanceId}");
            _appearances[e.AppearanceId] = e.Appearance;
        }
    }
}
