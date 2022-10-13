using OpenDreamShared.Dream;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;

namespace OpenDreamClient.Rendering {
    sealed class ClientAppearanceSystem : SharedAppearanceSystem {
        private Dictionary<uint, IconAppearance> _appearances = new();
        private readonly Dictionary<uint, List<Action<IconAppearance>>> _appearanceLoadCallbacks = new();
        private readonly Dictionary<uint, DreamIcon> _turfIcons = new();

        /// <summary>
        /// Holds the entities used by opaque turfs to block vision
        /// </summary>
        private readonly Dictionary<(IMapGrid, Vector2i), EntityUid> _opaqueTurfEntities = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize() {
            SubscribeNetworkEvent<AllAppearancesEvent>(OnAllAppearances);
            SubscribeNetworkEvent<NewAppearanceEvent>(OnNewAppearance);
            SubscribeNetworkEvent<AnimationEvent>(OnAnimation);
            SubscribeLocalEvent<GridModifiedEvent>(OnGridModified);
        }

        public override void Shutdown() {
            _appearances.Clear();
            _appearanceLoadCallbacks.Clear();
            _turfIcons.Clear();
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

        public DreamIcon GetTurfIcon(uint turfId) {
            uint appearanceId = turfId - 1;

            if (!_turfIcons.TryGetValue(appearanceId, out var icon)) {
                icon = new DreamIcon(appearanceId);
                _turfIcons.Add(appearanceId, icon);
            }

            return icon;
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

        private void OnAnimation(AnimationEvent e) {
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(e.Entity, out var sprite))
                return;

            LoadAppearance(e.TargetAppearanceId, targetAppearance => {
                sprite.Icon.StartAppearanceAnimation(targetAppearance, e.Duration);
            });
        }

        private void OnGridModified(GridModifiedEvent e) {
            foreach (var modified in e.Modified) {
                UpdateTurfOpacity(e.Grid, modified.position, modified.tile);
            }
        }

        private void UpdateTurfOpacity(IMapGrid grid, Vector2i position, Tile newTile) {
            LoadAppearance((uint)newTile.TypeId - 1, appearance => {
                bool hasOpaqueEntity = _opaqueTurfEntities.TryGetValue((grid, position), out var opaqueEntity);

                if (appearance.Opacity && !hasOpaqueEntity) {
                    var entityPosition = grid.GridTileToWorld(position);

                    // TODO: Maybe use a prototype?
                    opaqueEntity = _entityManager.SpawnEntity(null, entityPosition);
                    _entityManager.GetComponent<TransformComponent>(opaqueEntity).Anchored = true;
                    var occluder = _entityManager.AddComponent<ClientOccluderComponent>(opaqueEntity);
                    occluder.BoundingBox = Box2.FromDimensions(-1.0f, -1.0f, 1.0f, 1.0f);
                    occluder.Enabled = true;

                    _opaqueTurfEntities.Add((grid, position), opaqueEntity);
                } else if (hasOpaqueEntity) {
                    _entityManager.DeleteEntity(opaqueEntity);
                    _opaqueTurfEntities.Remove((grid, position));
                }
            });
        }
    }
}
