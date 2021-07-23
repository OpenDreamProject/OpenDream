using Content.Client.Resources;
using OpenDreamClient.Rendering;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Rendering {
    class DreamMapOverlay : Overlay {
        private IComponentManager _componentManager = IoCManager.Resolve<IComponentManager>();
        private IResourceCache _resourceCache = IoCManager.Resolve<IResourceCache>();
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleWorld handle = args.WorldHandle;
            ITransformComponent eyeTransform = _playerManager.LocalPlayer.Session.AttachedEntity.Transform;

            foreach (DMISpriteComponent sprite in _componentManager.EntityQuery<DMISpriteComponent>()) {
                ITransformComponent transform = sprite.Owner.Transform;

                if (transform.MapID != eyeTransform.MapID) //Only render our z-level
                    continue;

                if (_resourceCache.TryGetResource(sprite.Icon, out DMIResource dmi)) {
                    DMIResource.State dmiState = dmi.States[sprite.IconState];
                    
                    handle.DrawTexture(dmiState.Frames[Shared.Dream.AtomDirection.South][0], transform.WorldPosition);
                }
            }
        }
    }
}
