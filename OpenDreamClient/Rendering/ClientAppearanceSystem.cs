﻿using OpenDreamShared.Dream;
using SharedAppearanceSystem = OpenDreamShared.Rendering.SharedAppearanceSystem;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using Robust.Shared.Timing;

namespace OpenDreamClient.Rendering;

internal sealed class ClientAppearanceSystem : SharedAppearanceSystem {
    private Dictionary<uint, ImmutableAppearance> _appearances = new();
    private readonly Dictionary<uint, List<Action<ImmutableAppearance>>> _appearanceLoadCallbacks = new();
    private readonly Dictionary<uint, DreamIcon> _turfIcons = new();
    private readonly Dictionary<DreamFilter, ShaderInstance> _filterShaders = new();
    private bool _receivedAllAppearancesMsg;

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IDreamResourceManager _dreamResourceManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly DMISpriteSystem _spriteSystem = default!;

    public override void Initialize() {
        SubscribeNetworkEvent<NewAppearanceEvent>(OnNewAppearance);
        SubscribeNetworkEvent<RemoveAppearanceEvent>(e => _appearances.Remove(e.AppearanceId));
        SubscribeNetworkEvent<AnimationEvent>(OnAnimation);
        SubscribeLocalEvent<DMISpriteComponent, WorldAABBEvent>(OnWorldAABB);
    }

    public override void Shutdown() {
        _receivedAllAppearancesMsg = false;
        _appearances.Clear();
        _appearanceLoadCallbacks.Clear();
        _turfIcons.Clear();
        _filterShaders.Clear();
    }

    public void SetAllAppearances(Dictionary<uint, ImmutableAppearance> appearances) {
        _appearances = appearances;
        _receivedAllAppearancesMsg = true;

        //need to do this because all overlays can't be resolved until the whole appearance table is populated
        foreach(KeyValuePair<uint, ImmutableAppearance> pair in _appearances) {
            pair.Value.ResolveOverlays(this);
        }

        // Callbacks called in another pass to ensure all appearances are initialized first
        foreach (var callbackPair in _appearanceLoadCallbacks) {
            if (!_appearances.TryGetValue(callbackPair.Key, out var appearance))
                continue;

            foreach (var callback in callbackPair.Value)
                callback(appearance);
        }
    }

    public void LoadAppearance(uint appearanceId, Action<ImmutableAppearance> loadCallback) {
        if (_appearances.TryGetValue(appearanceId, out var appearance) && _receivedAllAppearancesMsg) {
            loadCallback(appearance);
            return;
        }

        if (!_appearanceLoadCallbacks.ContainsKey(appearanceId)) {
            _appearanceLoadCallbacks.Add(appearanceId, new());
        }

        _appearanceLoadCallbacks[appearanceId].Add(loadCallback);
    }

    public DreamIcon GetTurfIcon(uint turfId) {
        uint appearanceId = turfId;

        if (!_turfIcons.TryGetValue(appearanceId, out var icon)) {
            icon = new DreamIcon(_spriteSystem.RenderTargetPool, _gameTiming, _clyde, this, appearanceId);
            _turfIcons.Add(appearanceId, icon);
        }

        return icon;
    }

    public void OnNewAppearance(NewAppearanceEvent e) {
        uint appearanceId = e.Appearance.MustGetId();
        _appearances[appearanceId] = e.Appearance;

        // If we haven't received the MsgAllAppearances yet, leave this initialization for later
        if (_receivedAllAppearancesMsg) {
            _appearances[appearanceId].ResolveOverlays(this);

            if (_appearanceLoadCallbacks.TryGetValue(appearanceId, out var callbacks)) {
                foreach (var callback in callbacks) callback(_appearances[appearanceId]);
            }
        }
    }

    private void OnAnimation(AnimationEvent e) {
        if(e.Entity == NetEntity.Invalid && e.TurfId is not null) { //it's a turf or area
            if(_turfIcons.TryGetValue(e.TurfId.Value-1, out var turfIcon))
                LoadAppearance(e.TargetAppearanceId, targetAppearance => {
                    turfIcon.StartAppearanceAnimation(targetAppearance, e.Duration, e.Easing, e.Loop, e.Flags, e.Delay, e.ChainAnim);
                });
        } else { //image or movable
            EntityUid ent = _entityManager.GetEntity(e.Entity);
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(ent, out var sprite))
                return;

            LoadAppearance(e.TargetAppearanceId, targetAppearance => {
                sprite.Icon.StartAppearanceAnimation(targetAppearance, e.Duration, e.Easing, e.Loop, e.Flags, e.Delay, e.ChainAnim);
            });
        }
    }

    private void OnWorldAABB(EntityUid uid, DMISpriteComponent comp, ref WorldAABBEvent e) {
        Box2? aabb = null;

        comp.Icon.GetWorldAABB(_transformSystem.GetWorldPosition(uid), ref aabb);
        if (aabb != null)
            e.AABB = aabb.Value;
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
                case DreamFilterColor color: {
                    //Since SWSL doesn't support 4x5 matrices, we need to get a bit silly.
                    instance.SetParameter("colorMatrix", color.Color.GetMatrix4());
                    instance.SetParameter("offsetVector", color.Color.GetOffsetVector());
                    //TODO: Support the alternative colour mappings.
                    break;
                }
                case DreamFilterDisplace displace:
                    instance.SetParameter("size", displace.Size);
                    instance.SetParameter("x", displace.X);
                    instance.SetParameter("y", displace.Y);
                    break;
                case DreamFilterDropShadow dropShadow:
                    instance.SetParameter("size", dropShadow.Size);
                    instance.SetParameter("x", dropShadow.X);
                    instance.SetParameter("y", dropShadow.Y);
                    instance.SetParameter("shadow_color", dropShadow.Color);
                    // TODO: offset
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

        // Texture parameters need reset because different render targets can be used each frame
        switch (filter) {
            case DreamFilterAlpha alpha:
                if (!string.IsNullOrEmpty(alpha.RenderSource) && renderSourceLookup.TryGetValue(alpha.RenderSource, out var renderSourceTexture))
                    instance.SetParameter("mask_texture", renderSourceTexture.Texture);
                else if (alpha.Icon != 0) {
                    _dreamResourceManager.LoadResourceAsync<DMIResource>(alpha.Icon, rsc => {
                        instance.SetParameter("mask_texture", rsc.Texture);
                    });
                } else {
                    instance.SetParameter("mask_texture", Texture.Transparent);
                }

                break;
            case DreamFilterDisplace displace:
                if (!string.IsNullOrEmpty(displace.RenderSource) && renderSourceLookup.TryGetValue(displace.RenderSource, out renderSourceTexture)) {
                    instance.SetParameter("displacement_map", renderSourceTexture.Texture);
                } else if (displace.Icon != 0) {
                    _dreamResourceManager.LoadResourceAsync<DMIResource>(displace.Icon, rsc => {
                        instance.SetParameter("displacement_map", rsc.Texture);
                    });
                } else {
                    instance.SetParameter("displacement_map", Texture.Transparent);
                }

                break;
        }

        filter.Used = true;
        _filterShaders[filter] = instance;
        return instance;
    }

    public override ImmutableAppearance MustGetAppearanceById(uint appearanceId) {
        return _appearances[appearanceId];
    }

    public override void RemoveAppearance(ImmutableAppearance appearance) {
        throw new NotImplementedException();
    }
}
