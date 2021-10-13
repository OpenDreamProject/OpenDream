using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    class DMISpriteComponent : SharedDMISpriteComponent, ILookupWorldBox2Component {
        [ViewVariables] public DreamIcon Icon { get; set; } = new DreamIcon();
        [ViewVariables] public ScreenLocation ScreenLocation { get; set; } = null;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DMISpriteComponentState state = (DMISpriteComponentState)curState;

            ScreenLocation = state.ScreenLocation;
            if (state.AppearanceId != null) {
                Icon.SetAppearance(state.AppearanceId.Value);
            } else {
                Icon = null;
            }
        }

        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null) {
            Vector2 position = (worldPos ?? Vector2.Zero) + (0.5f, 0.5f);
            //TODO: Unit size is likely stored somewhere, use that instead of hardcoding 32
            Vector2 size = (Icon?.DMI?.IconSize ?? Vector2.Zero) / (32, 32) / 2;

            return new Box2(position, position + size);
        }

        public bool IsMouseOver(Vector2 position) {
            //TODO: mouse_opacity
            return true;
        }
    }
}
