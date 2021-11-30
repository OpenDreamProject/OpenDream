using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace OpenDreamClient.Rendering {
    class DreamScreenOverlay : Overlay {
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        protected override void Draw(in OverlayDrawArgs args) {
            DrawingHandleScreen handle = args.ScreenHandle;
            ClientScreenOverlaySystem screenOverlaySystem = EntitySystem.Get<ClientScreenOverlaySystem>();
            Vector2 viewportScale = args.ViewportBounds.Size / 480f; //TODO: Don't hardcode 480x480
            Matrix3 transform = Matrix3.CreateTransform(args.ViewportBounds.TopLeft, Angle.Zero, viewportScale);

            handle.SetTransform(transform);
            foreach (DMISpriteComponent sprite in screenOverlaySystem.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false)) continue;

                Vector2 position = sprite.ScreenLocation.GetScreenCoordinates(EyeManager.PixelsPerMeter);
                position.Y = (480 - position.Y);

                sprite.Icon.Draw(handle, position);
            }
        }
    }
}
