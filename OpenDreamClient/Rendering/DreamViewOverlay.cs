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
        private RenderOrderComparer _renderOrderComparer = new RenderOrderComparer();

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleWorld handle = args.WorldHandle;
            IEntity eye = _playerManager.LocalPlayer.Session.AttachedEntity;

            DrawMap(handle, eye);
            DrawScreenObjects(handle, eye, args.WorldAABB);
        }

        private void DrawMap(DrawingHandleWorld handle, IEntity eye) {
            List<DMISpriteComponent> sprites = new();

            foreach (IEntity entity in _entityLookup.GetEntitiesInRange(eye, 15)) {
                if (!entity.TryGetComponent(out DMISpriteComponent sprite))
                    continue;
                if (!sprite.IsVisible())
                    continue;

                sprites.Add(sprite);
            }

            sprites.Sort(_renderOrderComparer);
            foreach (DMISpriteComponent sprite in sprites) {
                sprite.Icon.Draw(handle, sprite.Owner.Transform.WorldPosition - 0.5f);
            }
        }

        private void DrawScreenObjects(DrawingHandleWorld handle, IEntity eye, Box2 worldAABB) {
            ClientScreenOverlaySystem screenOverlaySystem = EntitySystem.Get<ClientScreenOverlaySystem>();
            Vector2 viewOffset = eye.Transform.WorldPosition - (worldAABB.Size / 2f);

            foreach (DMISpriteComponent sprite in screenOverlaySystem.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false)) continue;

                Vector2 position = sprite.ScreenLocation.GetViewPosition(viewOffset, EyeManager.PixelsPerMeter);
                sprite.Icon.Draw(handle, position);
            }
        }
    }
}
