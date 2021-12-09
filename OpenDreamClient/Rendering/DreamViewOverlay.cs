using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace OpenDreamClient.Rendering {
    class DreamViewOverlay : Overlay {
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private IEntityLookup _entityLookup = IoCManager.Resolve<IEntityLookup>();
        private IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();
        private RenderOrderComparer _renderOrderComparer = new RenderOrderComparer();

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleWorld handle = args.WorldHandle;
            EntityUid? eye = _playerManager.LocalPlayer.Session.AttachedEntity;
            if (eye == null) return;

            DrawMap(handle, eye.Value);
            DrawScreenObjects(handle, eye.Value, args.WorldAABB);
        }

        private void DrawMap(DrawingHandleWorld handle, EntityUid eye) {
            List<DMISpriteComponent> sprites = new();

            foreach (EntityUid entity in _entityLookup.GetEntitiesInRange(eye, 15)) {
                if (!_entityManager.TryGetComponent<DMISpriteComponent>(entity, out var sprite))
                    continue;
                if (!sprite.IsVisible())
                    continue;

                sprites.Add(sprite);
            }

            sprites.Sort(_renderOrderComparer);
            foreach (DMISpriteComponent sprite in sprites) {
                if (!_entityManager.TryGetComponent<TransformComponent>(sprite.Owner, out var spriteTransform))
                    continue;

                DrawIcon(handle, sprite.Icon, spriteTransform.WorldPosition - 0.5f);
            }
        }

        private void DrawScreenObjects(DrawingHandleWorld handle, EntityUid eye, Box2 worldAABB) {
            if (!_entityManager.TryGetComponent<TransformComponent>(eye, out var eyeTransform))
                return;

            ClientScreenOverlaySystem screenOverlaySystem = EntitySystem.Get<ClientScreenOverlaySystem>();

            Vector2 viewOffset = eyeTransform.WorldPosition - (worldAABB.Size / 2f);

            List<DMISpriteComponent> sprites = new();
            foreach (DMISpriteComponent sprite in screenOverlaySystem.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false)) continue;

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
