using Content.Server.Dream;
using Content.Shared;
using Content.Shared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server {
    [RegisterComponent]
    class DMISpriteComponent : SharedDMISpriteComponent {
        private static readonly ResourcePath GamePath = new ResourcePath("/Game");

        private ResourcePath _icon = null;
        private string _iconState = null;
        private AtomDirection _direction = AtomDirection.South;
        private Vector2i _pixelOffset = Vector2i.Zero;
        private Color _color = Color.White;
        private float _layer = 0.0f;

        [ViewVariables]
        public ResourcePath Icon {
            get => _icon;
            set {
                _icon = GamePath / value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string IconState {
            get => _iconState;
            set {
                _iconState = value;
                Dirty();
            }
        }

        [ViewVariables]
        public AtomDirection Direction {
            get => _direction;
            set {
                _direction = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2i PixelOffset {
            get => _pixelOffset;
            set {
                _pixelOffset = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color {
            get => _color;
            set {
                _color = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Layer {
            get => _layer;
            set {
                _layer = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player) {
            return new DMISpriteComponentState(Icon, IconState, Direction, PixelOffset, Color, Layer);
        }

        public void SetAppearanceFromAtom(DreamObject atom) {
            if (atom.GetVariable("icon").TryGetValueAsDreamResource(out DreamResource icon)) {
                Icon = new ResourcePath(icon.ResourcePath);
            }

            if (atom.GetVariable("icon_state").TryGetValueAsString(out string iconState)) {
                IconState = iconState;
            }

            if (atom.GetVariable("color").TryGetValueAsString(out string color)) {
                Color = DreamColors.GetColor(color);
            }

            if (atom.GetVariable("dir").TryGetValueAsInteger(out int dir)) {
                Direction = (AtomDirection)dir;
            }

            if (atom.GetVariable("invisibility").TryGetValueAsInteger(out int invisibility)) {
                //TODO
            }

            if (atom.GetVariable("mouse_opacity").TryGetValueAsInteger(out int mouseOpacity)) {
                //TODO
            }

            atom.GetVariable("pixel_x").TryGetValueAsInteger(out int pixelX);
            atom.GetVariable("pixel_y").TryGetValueAsInteger(out int pixelY);
            PixelOffset = new Vector2i(pixelX, pixelY);

            if (atom.GetVariable("layer").TryGetValueAsFloat(out float layer)) {
                Layer = layer;
            }
        }
    }
}
