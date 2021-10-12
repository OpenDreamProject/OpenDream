using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace OpenDreamClient.Rendering {
    class DreamMapOverlay : Overlay {
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private IEntityLookup _entityLookup = IoCManager.Resolve<IEntityLookup>();
        private RenderOrderComparer _renderOrderComparer = new RenderOrderComparer();

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleWorld handle = args.WorldHandle;
            IEntity eye = _playerManager.LocalPlayer.Session.AttachedEntity;
            List<DMISpriteComponent> sprites = new();

            foreach (IEntity entity in _entityLookup.GetEntitiesInRange(eye, 15)) {
                if (!entity.TryGetComponent(out DMISpriteComponent sprite))
                    continue;

                sprites.Add(sprite);
            }

            sprites.Sort(_renderOrderComparer);
            foreach (DMISpriteComponent sprite in sprites) {
                if (sprite.Icon != null) {
                    RenderIcon(handle, sprite.Owner.Transform.WorldPosition, sprite.Icon);
                }
            }
        }

        private void RenderIcon(DrawingHandleWorld handle, Vector2 position, DreamIcon icon) {
            //TODO: mob.see_invisibility
            if (icon.Appearance.Invisibility > 0) return;

            position += icon.Appearance.PixelOffset / new Vector2(32, 32); //TODO: Unit size is likely stored somewhere, use that instead of hardcoding 32

            foreach (DreamIcon underlay in icon.Underlays) {
                RenderIcon(handle, position, underlay);
            }

            AtlasTexture frame = icon.CurrentFrame;
            if (frame != null) {
                handle.DrawTexture(frame, position, icon.Appearance.Color);
            }

            foreach (DreamIcon overlay in icon.Overlays) {
                RenderIcon(handle, position, overlay);
            }
        }
    }
}
