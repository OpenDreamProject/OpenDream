using Content.Client.Resources;
using OpenDreamClient.Rendering;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client.Rendering {
    class DreamMapOverlay : Overlay {
        private IComponentManager _componentManager = IoCManager.Resolve<IComponentManager>();
        private IResourceCache _resourceCache = IoCManager.Resolve<IResourceCache>();
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private RenderOrderComparer _renderOrderComparer = new RenderOrderComparer();

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleWorld handle = args.WorldHandle;
            ITransformComponent eyeTransform = _playerManager.LocalPlayer.Session.AttachedEntity.Transform;
            List<DMISpriteComponent> sprites = _componentManager.EntityQuery<DMISpriteComponent>().ToList();

            sprites.Sort(_renderOrderComparer);
            foreach (DMISpriteComponent sprite in sprites) {
                ITransformComponent transform = sprite.Owner.Transform;

                if (transform.MapID != eyeTransform.MapID) //Only render our z-level
                    continue;

                if (sprite.DMI != null && sprite.IconState != null && sprite.DMI.States.TryGetValue(sprite.IconState, out var dmiState)) {
                    AtlasTexture[] frames = dmiState.GetFrames(sprite.Direction);
                    Vector2 position = transform.WorldPosition;
                    position += sprite.PixelOffset / new Vector2(32, 32); //TODO: Unit size is likely stored somewhere, use that instead of hardcoding 32
                    
                    handle.DrawTexture(frames[0], position, sprite.Color);
                }
            }
        }
    }
}
