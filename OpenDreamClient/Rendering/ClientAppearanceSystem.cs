using OpenDreamShared.Dream;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
namespace OpenDreamClient.Rendering {
    sealed class ClientAppearanceSystem : SharedAppearanceSystem {
        private Dictionary<uint, IconAppearance> _appearances = new();
        private readonly Dictionary<uint, List<Action<IconAppearance>>> _appearanceLoadCallbacks = new();
        private readonly Dictionary<uint, DreamIcon> _turfIcons = new();
        private readonly Dictionary<DreamFilter, ShaderInstance> _filterShaders = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize() {
            SubscribeNetworkEvent<AllAppearancesEvent>(OnAllAppearances);
            SubscribeNetworkEvent<NewAppearanceEvent>(OnNewAppearance);
            SubscribeNetworkEvent<AnimationEvent>(OnAnimation);
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

        public void ResetFilterUsageCounts()
        {
            foreach(DreamFilter key in _filterShaders.Keys)
            {
                key.used = false;
            }
        }

        public void CleanUpUnusedFilters()
        {
            foreach(DreamFilter key in _filterShaders.Keys)
            {
                if(!key.used)
                    _filterShaders.Remove(key);
            }
        }

        public ShaderInstance GetFilterShader(DreamFilter filter)
        {
            ShaderInstance instance = null;
            if(!_filterShaders.TryGetValue(filter, out instance))
            {
                var _protoManager = IoCManager.Resolve<IPrototypeManager>();
                instance = _protoManager.Index<ShaderPrototype>(filter.filter_type).InstanceUnique();

                if(filter.filter_x != null) instance.SetParameter("x", (float) filter.filter_x);
                if(filter.filter_y != null) instance.SetParameter("y", (float) filter.filter_y);
                if(filter.filter_icon != null) instance.SetParameter("icon", (float) filter.filter_icon);
                if(filter.filter_render_source != null) instance.SetParameter("render_source", (float) filter.filter_render_source);
                if(filter.filter_flags != null) instance.SetParameter("flags", (float) filter.filter_flags);
                if(filter.filter_size != null) instance.SetParameter("size", (float) filter.filter_size);
                if(filter.filter_color_string != null)
                {
                    if (!ColorHelpers.TryParseColor(filter.filter_color_string, out var c)) {
                        throw new Exception("bad color");
                    }
                    instance.SetParameter("color", c);
                }
                if(filter.filter_threshold_color != null)
                {
                    if (!ColorHelpers.TryParseColor(filter.filter_threshold_color, out var c)) {
                        throw new Exception("bad color");
                    }
                    instance.SetParameter("color", c);
                }
                if(filter.filter_threshold_strength != null) instance.SetParameter("threshold_strength", (float) filter.filter_threshold_strength);
                if(filter.filter_offset != null) instance.SetParameter("offset", (float) filter.filter_offset);
                if(filter.filter_alpha != null) instance.SetParameter("alpha", (float) filter.filter_alpha);
                if(filter.filter_color_matrix != null) instance.SetParameter("color_matrix", (float) filter.filter_color_matrix);
                if(filter.filter_space != null) instance.SetParameter("space", (float) filter.filter_space);
                if(filter.filter_transform != null) instance.SetParameter("transform", (float) filter.filter_transform);
                if(filter.filter_blend_mode != null) instance.SetParameter("blend_mode", (float) filter.filter_blend_mode);
                if(filter.filter_density != null) instance.SetParameter("density", (float) filter.filter_density);
                if(filter.filter_factor != null) instance.SetParameter("factor", (float) filter.filter_factor);
                if(filter.filter_repeat != null) instance.SetParameter("repeat", (float) filter.filter_repeat);
                if(filter.filter_radius != null) instance.SetParameter("radius", (float) filter.filter_radius);
                if(filter.filter_falloff != null) instance.SetParameter("falloff", (float) filter.filter_falloff);
            }
            filter.used = true;
            _filterShaders[filter] = instance;
            return instance;
        }
    }
}

