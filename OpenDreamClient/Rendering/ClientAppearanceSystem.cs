using OpenDreamShared.Dream;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;

namespace OpenDreamClient.Rendering {
    sealed class ClientAppearanceSystem : SharedAppearanceSystem {
        private Dictionary<uint, IconAppearance> _appearances = new();
        private readonly Dictionary<uint, List<Action<IconAppearance>>> _appearanceLoadCallbacks = new();
        private readonly Dictionary<uint, DreamIcon> _turfIcons = new();
        private readonly Dictionary<DreamFilter, ShaderInstance> _filterShaders = new();

        /// <summary>
        /// Holds the entities used by opaque turfs to block vision
        /// </summary>
        private readonly Dictionary<(MapGridComponent, Vector2i), EntityUid> _opaqueTurfEntities = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly OccluderSystem _occluderSystem = default!;
        [Dependency] private readonly IDreamResourceManager _dreamResourceManager = default!;

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

        public void ResetFilterUsageFlags() {
            foreach (DreamFilter key in _filterShaders.Keys) {
                key.Used = false;
            }
        }

        public void CleanUpUnusedFilters() {
            foreach (DreamFilter key in _filterShaders.Keys) {
                if (!key.Used)
                    _filterShaders.Remove(key);
            }
        }

        public ShaderInstance GetFilterShader(DreamFilter filter, Dictionary<string, IRenderTexture> renderSourceLookup) {
            if (!_filterShaders.TryGetValue(filter, out var instance)) {
                var protoManager = IoCManager.Resolve<IPrototypeManager>();

                instance = protoManager.Index<ShaderPrototype>(filter.FilterType).InstanceUnique();

                switch (filter) {
                    case DreamFilterAlpha alpha:
                        if(!String.IsNullOrEmpty(alpha.RenderSource) && renderSourceLookup.TryGetValue(alpha.RenderSource, out var renderSourceTexture))
                            instance.SetParameter("mask_texture", renderSourceTexture.Texture);
                        else if(alpha.Icon != 0){
                            _dreamResourceManager.LoadResourceAsync<DMIResource>(alpha.Icon, (DMIResource rsc) => {
                                    instance.SetParameter("mask_texture", rsc.Texture);
                                });
                        }
                        instance.SetParameter("x",alpha.X);
                        instance.SetParameter("y",alpha.Y);
                        instance.SetParameter("flags",alpha.Flags);
                        break;
                    case DreamFilterAngularBlur angularBlur:
                        break;
                    case DreamFilterBloom bloom:
                        break;
                    case DreamFilterBlur blur:
                        instance.SetParameter("size", blur.Size);
                        break;
                    case DreamFilterColor color:
                        break;
                    case DreamFilterDisplace displace:
                        break;
                    case DreamFilterDropShadow dropShadow:
                        break;
                    case DreamFilterLayer layer:
                        break;
                    case DreamFilterMotionBlur motionBlur:
                        break;
                    case DreamFilterOutline outline:
                        instance.SetParameter("size", outline.Size);
                        instance.SetParameter("color", outline.Color);
                        instance.SetParameter("flags", outline.Flags);
                        break;
                    case DreamFilterRadialBlur radialBlur:
                        break;
                    case DreamFilterRays rays:
                        break;
                    case DreamFilterRipple ripple:
                        break;
                    case DreamFilterWave wave:
                        break;
                    case DreamFilterGreyscale greyscale:
                        break;
                }
            }

            filter.Used = true;
            _filterShaders[filter] = instance;
            return instance;
        }

        private void OnGridModified(GridModifiedEvent e) {
            foreach (var modified in e.Modified) {
                UpdateTurfOpacity(e.Grid, modified.position, modified.tile);
            }
        }

        private void UpdateTurfOpacity(MapGridComponent grid, Vector2i position, Tile newTile) {
            LoadAppearance((uint)newTile.TypeId - 1, appearance => {
                bool hasOpaqueEntity = _opaqueTurfEntities.TryGetValue((grid, position), out var opaqueEntity);

                if (appearance.Opacity && !hasOpaqueEntity) {
                    var entityPosition = grid.GridTileToWorld(position);

                    // TODO: Maybe use a prototype?
                    opaqueEntity = _entityManager.SpawnEntity(null, entityPosition);
                    _entityManager.GetComponent<TransformComponent>(opaqueEntity).Anchored = true;
                    var occluder = _entityManager.AddComponent<OccluderComponent>(opaqueEntity);
                    _occluderSystem.SetBoundingBox(opaqueEntity, Box2.FromDimensions(-1.0f, -1.0f, 1.0f, 1.0f), occluder);
                    _occluderSystem.SetEnabled(opaqueEntity, true, occluder);

                    _opaqueTurfEntities.Add((grid, position), opaqueEntity);
                } else if (hasOpaqueEntity) {
                    _entityManager.DeleteEntity(opaqueEntity);
                    _opaqueTurfEntities.Remove((grid, position));
                }
            });
        }
    }
}

