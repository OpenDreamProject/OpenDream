using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace OpenDreamClient.Rendering {
    sealed class DreamViewOverlay : Overlay {
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private IEntitySystemManager _entitySystem = IoCManager.Resolve<IEntitySystemManager>();
        private IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();
        private IMapManager _mapManager = IoCManager.Resolve<IMapManager>();
        private RenderOrderComparer _renderOrderComparer = new RenderOrderComparer();
        [CanBeNull] private EntityLookupSystem _lookupSystem;
        [CanBeNull] private SharedTransformSystem _transformSystem;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
            if (eye == null) return;

            DrawingHandleWorld handle = args.WorldHandle;
            DrawMap(args, eye.Value);
            DrawScreenObjects(handle, eye.Value, args.WorldAABB);
        }

        private void DrawMap(OverlayDrawArgs args, EntityUid eye) {
            _transformSystem ??= _entitySystem.GetEntitySystem<SharedTransformSystem>();
            _lookupSystem ??= _entitySystem.GetEntitySystem<EntityLookupSystem>();
            var spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

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
        }

        private void DrawIcon(DrawingHandleWorld handle, DreamIcon icon, Vector2 position) {
            position += icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;

            foreach (DreamIcon underlay in icon.Underlays) {
                DrawIcon(handle, underlay, position);
            }

            AtlasTexture frame = icon.CurrentFrame;
            if (frame != null) {
                handle.DrawTexture(frame, position, icon.Appearance.Color);
            }

            foreach (DreamIcon overlay in icon.Overlays) {
                DrawIcon(handle, overlay, position);
            }
        }
    }
}
