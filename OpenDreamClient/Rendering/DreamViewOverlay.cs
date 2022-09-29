using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;

namespace OpenDreamClient.Rendering {
    sealed class DreamViewOverlay : Overlay {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly RenderOrderComparer _renderOrderComparer = new RenderOrderComparer();
        private EntityLookupSystem _lookupSystem;
        private ClientAppearanceSystem _appearanceSystem;
        private SharedTransformSystem _transformSystem;
        private IClydeViewport _vp;
        private Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new Dictionary<Vector2i, List<IRenderTexture>>();
        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
        private ClientAppearanceSystem appearanceSystem;

        public DreamViewOverlay() {
            IoCManager.InjectDependencies(this);
        }

        protected override void Draw(in OverlayDrawArgs args) {
            appearanceSystem = EntitySystem.Get<ClientAppearanceSystem>();
            EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
            if (eye == null) return;

            DrawingHandleWorld handle = args.WorldHandle;
            _vp = args.Viewport;
            DrawMap(args, eye.Value);
            DrawScreenObjects(handle, eye.Value, args.WorldAABB);

        }

        private void DrawMap(OverlayDrawArgs args, EntityUid eye) {
            _transformSystem ??= _entitySystem.GetEntitySystem<SharedTransformSystem>();
            _lookupSystem ??= _entitySystem.GetEntitySystem<EntityLookupSystem>();
            _appearanceSystem ??= _entitySystem.GetEntitySystem<ClientAppearanceSystem>();
            var spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

            if (!xformQuery.TryGetComponent(eye, out var eyeTransform))
                return;

            DrawTiles(args, eyeTransform);

            var entities = _lookupSystem.GetEntitiesIntersecting(args.MapId, args.WorldAABB);
            List<DMISpriteComponent> sprites = new(entities.Count + 1);

            if(spriteQuery.TryGetComponent(eye, out var player) && player.IsVisible(mapManager: _mapManager))
                sprites.Add(player);

            foreach (EntityUid entity in entities) {
                if (!spriteQuery.TryGetComponent(entity, out var sprite))
                    continue;
                if (!sprite.IsVisible(mapManager: _mapManager))
                    continue;

                sprites.Add(sprite);
            }

            sprites.Sort(_renderOrderComparer);
            foreach (DMISpriteComponent sprite in sprites) {
                if (!xformQuery.TryGetComponent(sprite.Owner, out var spriteTransform))
                    continue;

                DrawIcon(args.WorldHandle, sprite.Icon, _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f);
            }
        }

        private void DrawTiles(OverlayDrawArgs args, TransformComponent eyeTransform) {
            if (!_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
                return;

            foreach (TileRef tileRef in grid.GetTilesIntersecting(Box2.CenteredAround(eyeTransform.WorldPosition, (17, 17)))) {
                MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
                DreamIcon icon = _appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId);

                DrawIcon(args.WorldHandle, icon, pos.Position - 1);
            }
        }

        private void DrawScreenObjects(DrawingHandleWorld handle, EntityUid eye, Box2 worldAABB) {
            if (!_entityManager.TryGetComponent<TransformComponent>(eye, out var eyeTransform))
                return;

            ClientScreenOverlaySystem screenOverlaySystem = EntitySystem.Get<ClientScreenOverlaySystem>();

            Vector2 viewOffset = eyeTransform.WorldPosition - (worldAABB.Size / 2f);

            List<DMISpriteComponent> sprites = new();
            foreach (DMISpriteComponent sprite in screenOverlaySystem.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false, mapManager: _mapManager)) continue;

                sprites.Add(sprite);
            }

            sprites.Sort(_renderOrderComparer);
            foreach (DMISpriteComponent sprite in sprites) {
                Vector2 position = sprite.ScreenLocation.GetViewPosition(viewOffset, EyeManager.PixelsPerMeter);
                Vector2 iconSize = sprite.Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter;

                for (int x = 0; x < sprite.ScreenLocation.RepeatX; x++) {
                    for (int y = 0; y < sprite.ScreenLocation.RepeatY; y++) {
                        DrawIcon(handle, sprite.Icon, position + iconSize * (x, y));
                    }
                }
            }

            
            appearanceSystem.CleanUpUnusedFilters();
            appearanceSystem.ResetFilterUsageCounts();            
        }


        private IRenderTexture RentPingPongRenderTarget(Vector2i size)
        {
            List<IRenderTexture> listresult;
            IRenderTexture result;
            if(!_renderTargetCache.TryGetValue(size, out listresult))
                result = IoCManager.Resolve<IClyde>().CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb));
            else
            {
                if(listresult.Count > 0)
                {
                    result = listresult[0]; //pop a value
                    listresult.Remove(result);
                }
                else
                    result = IoCManager.Resolve<IClyde>().CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb));
                _renderTargetCache[size] = listresult; //put the shorter list back
            }
            return result;
        }

        private void ReturnPingPongRenderTarget(IRenderTexture rental)
        {
            List<IRenderTexture> storeList;
            if(!_renderTargetCache.TryGetValue(rental.Size, out storeList))
                storeList = new List<IRenderTexture>(4);
            storeList.Add(rental);
            _renderTargetCache[rental.Size]=storeList;
        }

        private void DrawIcon(DrawingHandleWorld handle, DreamIcon icon, Vector2 position) {
            position += icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;

            foreach (DreamIcon underlay in icon.Underlays) {
                DrawIcon(handle, underlay, position);
            }

            AtlasTexture frame = icon.CurrentFrame;
            if(frame != null && icon.Appearance.Filters.Count == 0)
            {
                //faster path for rendering unshaded sprites
                handle.DrawTexture(frame, position, icon.Appearance.Color);
            }
            else if (frame != null) {
                IRenderTexture ping = RentPingPongRenderTarget(frame.Size*2);
                IRenderTexture pong = RentPingPongRenderTarget(frame.Size*2);
                IRenderTexture tmpHolder;

                handle.RenderInRenderTarget(pong, () => {
                    handle.DrawTextureRect(frame, new Box2(Vector2.Zero+(frame.Size/2), frame.Size+(frame.Size/2)), icon.Appearance.Color);
                });
                bool rotate = true;
                foreach(DreamFilter filterID in icon.Appearance.Filters)
                {
                    ShaderInstance s = appearanceSystem.GetFilterShader(filterID);
                    handle.RenderInRenderTarget(ping, () => {
                        handle.DrawRect(new Box2(Vector2.Zero, frame.Size*2), new Color());
                        handle.UseShader(s);
                        handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size*2));
                        handle.UseShader(null);
                        });
                    tmpHolder = ping;
                    ping = pong;
                    pong = tmpHolder;
                    rotate = !rotate;
                }
                if(rotate) //this is so dumb
                {
                    handle.RenderInRenderTarget(ping, () => {
                        handle.DrawRect(new Box2(Vector2.Zero, frame.Size*2), new Color());
                        handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size*2));
                        });
                    tmpHolder = ping;
                    ping = pong;
                    pong = tmpHolder;
                }
                handle.DrawTexture(pong.Texture, position-((frame.Size/2)/(float)EyeManager.PixelsPerMeter), icon.Appearance.Color);
                ReturnPingPongRenderTarget(ping);
                ReturnPingPongRenderTarget(pong);
            }

            foreach (DreamIcon overlay in icon.Overlays) {
                DrawIcon(handle, overlay, position);
            }

        }
    }
}
