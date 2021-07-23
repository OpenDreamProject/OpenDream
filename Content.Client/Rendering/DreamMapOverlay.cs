using Content.Client.Resources;
using OpenDreamClient.Rendering;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Rendering {
    class DreamMapOverlay : Overlay {
        private IComponentManager _componentManager = IoCManager.Resolve<IComponentManager>();
        private IResourceCache _resourceCache = IoCManager.Resolve<IResourceCache>();

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleWorld handle = args.WorldHandle;

            foreach (DMISpriteComponent sprite in _componentManager.EntityQuery<DMISpriteComponent>()) {
                ITransformComponent transform = sprite.Owner.Transform;

                if (_resourceCache.TryGetResource(sprite.Icon, out DMIResource dmi)) {
                    DMIResource.State dmiState = dmi.States[sprite.IconState];

                    handle.DrawTexture(dmiState.Frames[Shared.Dream.AtomDirection.South][0], transform.WorldPosition);
                }
            }
        }
    }
}
