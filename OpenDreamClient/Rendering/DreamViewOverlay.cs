using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

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
        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

        public DreamViewOverlay() {
            IoCManager.InjectDependencies(this);
        }

        protected override void Draw(in OverlayDrawArgs args) {
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
                        handle.RenderInRenderTarget(_vp.RenderTarget,
                            () => DrawIcon(handle, sprite.Icon, position + iconSize * (x, y))
                        );
                    }
                }
            }
        }

        private void DrawIcon(DrawingHandleWorld handle, DreamIcon icon, Vector2 position) {
            position += icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;

            foreach (DreamIcon underlay in icon.Underlays) {
                DrawIcon(handle, underlay, position);
            }

            AtlasTexture frame = icon.CurrentFrame;
            if (frame != null) {

                IRenderTexture ping = IoCManager.Resolve<IClyde>().CreateRenderTarget(frame.Size,
                                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb));
                IRenderTexture pong = IoCManager.Resolve<IClyde>().CreateRenderTarget(frame.Size,
                                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb));  
                IRenderTexture tmpHolder;
            
                handle.RenderInRenderTarget(pong, () => {
                    handle.DrawTexture(frame, Vector2.Zero, icon.Appearance.Color);
                });
                
                foreach(ShaderInstance s in icon.Filters)
                {
                    handle.RenderInRenderTarget(ping, () => {
                        handle.DrawRect(new Box2(Vector2.Zero, ping.Size), new Color());
                        handle.UseShader(s);
                        handle.DrawTexture(pong.Texture, Vector2.Zero);
                        handle.UseShader(null);                    
                        });
                    tmpHolder = ping;
                    ping = pong;
                    pong = tmpHolder;                       
                }
                
                handle.DrawTexture(pong.Texture, position, icon.Appearance.Color);           
            }

            foreach (DreamIcon overlay in icon.Overlays) {
                DrawIcon(handle, overlay, position);
            }

        }
    }
}
