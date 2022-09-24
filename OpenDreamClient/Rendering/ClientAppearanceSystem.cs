﻿using OpenDreamShared.Dream;
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

        public ShaderInstance GetFilterShader(DreamFilter filter)
        {
            ShaderInstance instance = null;
            if(!_filterShaders.TryGetValue(filter, out instance))
            {
                var _protoManager = IoCManager.Resolve<IPrototypeManager>();
                instance = _protoManager.Index<ShaderPrototype>(filter.filter_type).InstanceUnique();
                if(filter.filter_size != null) instance.SetParameter("size",(float) filter.filter_size);
                if(filter.filter_flags != null) instance.SetParameter("flags",(float) filter.filter_flags);
                if(filter.filter_color != null) 
                {
                    if (!ColorHelpers.TryParseColor(filter.filter_color, out var c)) {
                        throw new Exception("bad color");
                    }
                    instance.SetParameter("color", c);
                }
                //instance.SetParameter("outline_color", new Vector4(1.0f,0f,0f,0.5f));
                _filterShaders.Add(filter, instance);
            }
            return instance;
        }
    }
}

