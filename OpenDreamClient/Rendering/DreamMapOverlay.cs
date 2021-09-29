using OpenDreamClient.Rendering;
using OpenDreamClient.Resources;
using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Linq;

namespace OpenDreamClient.Rendering {
    class DreamMapOverlay : Overlay {
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private IEntityLookup _entityLookup = IoCManager.Resolve<IEntityLookup>();
        private IDreamResourceManager _resourceManager = IoCManager.Resolve<IDreamResourceManager>();
        private RenderOrderComparer _renderOrderComparer = new RenderOrderComparer();

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleWorld handle = args.WorldHandle;
            IEntity eye = _playerManager.LocalPlayer.Session.AttachedEntity;
            List<DMISpriteComponent> sprites = new(); ;

            foreach (IEntity entity in _entityLookup.GetEntitiesInRange(eye, 15)) {
                if (!entity.TryGetComponent(out DMISpriteComponent sprite))
                    continue;

                sprites.Add(sprite);
            }

            sprites.Sort(_renderOrderComparer);
            foreach (DMISpriteComponent sprite in sprites) {
                ITransformComponent transform = sprite.Owner.Transform;

                //TODO
            }
        }
    }
}
