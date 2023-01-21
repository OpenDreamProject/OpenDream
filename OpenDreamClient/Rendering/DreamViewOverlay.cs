using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering world atoms
/// </summary>
sealed class DreamViewOverlay : Overlay {
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new();
    private readonly RenderOrderComparer _renderOrderComparer = new();
    private EntityLookupSystem _lookupSystem;
    private ClientAppearanceSystem _appearanceSystem;
    private SharedTransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
    private List<IRenderTarget> usedRenderTargets = new();


    public DreamViewOverlay() {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        //because we render everything in render targets, and then render those to the world, we've got to apply some transformations to all world draws
        //in order to correct for different coordinate systems and general weirdness
        args.WorldHandle.SetTransform(new Vector2(0,args.WorldAABB.Size.Y), Angle.FromDegrees(180), new Vector2(-1,1));
        DrawMap(args, eye.Value);
        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();
    }

    private void DrawMap(OverlayDrawArgs args, EntityUid eye) {
        _transformSystem ??= _entitySystem.GetEntitySystem<SharedTransformSystem>();
        _lookupSystem ??= _entitySystem.GetEntitySystem<EntityLookupSystem>();
        _appearanceSystem ??= _entitySystem.GetEntitySystem<ClientAppearanceSystem>();
        var spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(eye, out var eyeTransform))
            return;

        var entities = _lookupSystem.GetEntitiesIntersecting(args.MapId, args.WorldAABB);
        List<(DreamIcon, Vector2, EntityUid)> sprites = new(entities.Count + 1);

        if (spriteQuery.TryGetComponent(eye, out var player) && player.IsVisible(mapManager: _mapManager) && xformQuery.TryGetComponent(player.Owner, out var playerTransform))
            sprites.Add((player.Icon, _transformSystem.GetWorldPosition(playerTransform.Owner, xformQuery) - 0.5f, player.Owner));

        foreach (EntityUid entity in entities) {
            if (!spriteQuery.TryGetComponent(entity, out var sprite))
                continue;
            if (!sprite.IsVisible(mapManager: _mapManager))
                continue;
            if(!xformQuery.TryGetComponent(sprite.Owner, out var spriteTransform))
                continue;
            sprites.Add((sprite.Icon, _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f, sprite.Owner));
        }

        if (_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
            foreach (TileRef tileRef in grid.GetTilesIntersecting(Box2.CenteredAround(eyeTransform.WorldPosition, (17, 17)))) {
                MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
                sprites.Add((_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), pos.Position - 1, tileRef.GridUid));
            }

        //early return if there's nothing to do
        if(sprites.Count == 0)
            return;

        sprites.Sort(_renderOrderComparer);
        //After sort, group by plane and render together
        float lastPlane = sprites[0].Item1.Appearance.Plane;
        IRenderTexture planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        usedRenderTargets.Add(planeTarget);
        ClearRenderTarget(planeTarget, args.WorldHandle);
        foreach ((DreamIcon, Vector2, EntityUid) sprite in sprites) {
            if(lastPlane != sprite.Item1.Appearance.Plane){
                args.WorldHandle.DrawTexture(planeTarget.Texture, Vector2.Zero);
                lastPlane = sprite.Item1.Appearance.Plane;
                //usedRenderTargets.Add(planeTarget);
                //planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
                ClearRenderTarget(planeTarget, args.WorldHandle);
            }
            DrawIcon(args.WorldHandle, planeTarget, sprite.Item1, sprite.Item2);
        }
        //final draw
        args.WorldHandle.DrawTexture(planeTarget.Texture, Vector2.Zero);

        foreach(IRenderTexture used in usedRenderTargets){
            ReturnPingPongRenderTarget(used);
        }
        usedRenderTargets.Clear();
    }

    private IRenderTexture RentPingPongRenderTarget(Vector2i size) {
        IRenderTexture result;

        if (!_renderTargetCache.TryGetValue(size, out var listResult)) {
            result = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
        } else {
            if (listResult.Count > 0) {
                result = listResult[0]; //pop a value
                listResult.Remove(result);
            } else {
                result = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
            }

            _renderTargetCache[size] = listResult; //put the shorter list back
        }
        return result;
    }

    private void ReturnPingPongRenderTarget(IRenderTexture rental) {
        if (!_renderTargetCache.TryGetValue(rental.Size, out var storeList))
            storeList = new List<IRenderTexture>(4);

        storeList.Add(rental);
        _renderTargetCache[rental.Size] = storeList;
    }

    private void ClearRenderTarget(IRenderTexture target, DrawingHandleWorld handle)
    {
        handle.RenderInRenderTarget(target, () => {}, Color.Transparent);
    }

    // TODO: Move this to DreamIcon.Draw() so screen objects can have filters
    private void DrawIcon(DrawingHandleWorld handle, IRenderTarget renderTarget, DreamIcon icon, Vector2 position) {
        if (icon.Appearance == null)
            return;

        position += icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;
        Vector2 pixelPosition = position*EyeManager.PixelsPerMeter;
        //TODO appearance_flags - notably KEEP_TOGETHER and KEEP_APART
        //keep together can probably just be a subcall to DrawIcon()?

        //TODO check for images with override here

        //TODO vis_contents

        //underlays - inherit colour and transform ?
        foreach (DreamIcon underlay in icon.Underlays) {
            DrawIcon(handle, renderTarget, underlay, position);
        }

        //main icon - TODO transform
        AtlasTexture frame = icon.CurrentFrame;
        if(frame != null && icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.DrawTextureRect(frame,
                        new Box2(pixelPosition, pixelPosition+frame.Size),
                        icon.Appearance.Color);
                }, null);
        } else if (frame != null) {
            IRenderTexture ping = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture pong = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture tmpHolder;

            handle.RenderInRenderTarget(pong,
                () => {
                    handle.DrawTextureRect(frame,
                        new Box2(Vector2.Zero + (frame.Size / 2), frame.Size + (frame.Size / 2)),
                        icon.Appearance.Color);
                }, Color.Transparent);

            bool rotate = true;
            foreach (DreamFilter filterId in icon.Appearance.Filters) {
                ShaderInstance s = _appearanceSystem.GetFilterShader(filterId);

                handle.RenderInRenderTarget(ping, () => {
                    handle.DrawRect(new Box2(Vector2.Zero, frame.Size * 2), new Color());
                    handle.UseShader(s);
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                    handle.UseShader(null);
                }, Color.Transparent);

                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
                rotate = !rotate;
            }

            //this is so dumb
            if (rotate) {
                handle.RenderInRenderTarget(ping, () => {
                    handle.DrawRect(new Box2(Vector2.Zero, frame.Size * 2), new Color());
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                }, Color.Transparent);

                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
            }

            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.DrawTextureRect(pong.Texture,
                        new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                        icon.Appearance.Color);
                }, null);
            ReturnPingPongRenderTarget(ping);
            ReturnPingPongRenderTarget(pong);
        }

        //TODO maptext - note colour + transform apply

        //TODO particles - colour and transform don't apply?

        //overlays - colour and transform are inherited, but filters aren't
        foreach (DreamIcon overlay in icon.Overlays) {
            DrawIcon(handle, renderTarget, overlay, position);
        }

    }
}

