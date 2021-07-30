using Content.Client.Resources;
using Content.Shared;
using Content.Shared.Dream;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    class DMISpriteComponent : SharedDMISpriteComponent, ILookupWorldBox2Component {
        public DMIResource DMI { get; private set; }
        public ResourcePath Icon {
            get => _icon;
            set {
                _icon = value;
                if (_icon != null && _resourceCache.TryGetResource(_icon, out DMIResource dmi)) {
                    DMI = dmi;
                }
            }
        }
        private ResourcePath _icon;

        public string IconState { get; set; }
        public AtomDirection Direction { get; set; }
        public Vector2i PixelOffset { get; set; }
        public Color Color { get; set; }
        public float Layer { get; set; }

        private IResourceCache _resourceCache = IoCManager.Resolve<IResourceCache>();

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DMISpriteComponentState state = (DMISpriteComponentState)curState;
            Icon = state.Icon;
            IconState = state.IconState;
            Direction = state.Direction;
            PixelOffset = state.PixelOffset;
            Color = state.Color;
            Layer = state.Layer;
        }

        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null) {
            Vector2 position = (worldPos ?? Vector2.Zero) + (0.5f, 0.5f);
            //TODO: Unit size is likely stored somewhere, use that instead of hardcoding 32
            Vector2 size = (DMI?.IconSize ?? Vector2.Zero) / (32, 32) / 2;

            return new Box2(position, position + size);
        }

        public bool IsMouseOver(Vector2 position) {
            //TODO: mouse_opacity
            return true;
        }
    }
}
