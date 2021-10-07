using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace OpenDreamRuntime {
    [RegisterComponent]
    class DMISpriteComponent : SharedDMISpriteComponent {
        private uint? _appearanceId;

        [ViewVariables]
        public uint? AppearanceId {
            get => _appearanceId;
            set {
                _appearanceId = value;
                Dirty();
            }
        }

        [ViewVariables]
        public IconAppearance? Appearance {
            get => (AppearanceId != null) ? EntitySystem.Get<ServerAppearanceSystem>().GetAppearance(AppearanceId.Value) : null;
            set => AppearanceId = (value != null) ? EntitySystem.Get<ServerAppearanceSystem>().AddAppearance(value) : null;
        }

        public override ComponentState GetComponentState(ICommonSession player) {
            return new DMISpriteComponentState(AppearanceId);
        }

        public void SetAppearanceFromAtom(DreamObject atom) {
            IconAppearance appearance = new IconAppearance();

            if (atom.GetVariable("icon").TryGetValueAsDreamResource(out DreamResource icon)) {
                appearance.Icon = new ResourcePath(icon.ResourcePath);
            }

            if (atom.GetVariable("icon_state").TryGetValueAsString(out string iconState)) {
                appearance.IconState = iconState;
            }

            if (atom.GetVariable("color").TryGetValueAsString(out string color)) {
                appearance.SetColor(color);
            }

            if (atom.GetVariable("dir").TryGetValueAsInteger(out int dir)) {
                appearance.Direction = (AtomDirection)dir;
            }

            if (atom.GetVariable("invisibility").TryGetValueAsInteger(out int invisibility)) {
                appearance.Invisibility = invisibility;
            }

            if (atom.GetVariable("mouse_opacity").TryGetValueAsInteger(out int mouseOpacity)) {
                appearance.MouseOpacity = (MouseOpacity)mouseOpacity;
            }

            atom.GetVariable("pixel_x").TryGetValueAsInteger(out int pixelX);
            atom.GetVariable("pixel_y").TryGetValueAsInteger(out int pixelY);
            appearance.PixelOffset = new Vector2i(pixelX, pixelY);

            if (atom.GetVariable("layer").TryGetValueAsFloat(out float layer)) {
                appearance.Layer = layer;
            }

            Appearance = appearance;
        }
    }
}
