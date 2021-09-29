using OpenDreamClient.Resources;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    class DMISpriteComponent : SharedDMISpriteComponent, ILookupWorldBox2Component {
        public uint? AppearanceId { get; set; }

        [Dependency]
        private IDreamResourceManager _resourceManager = default!;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DMISpriteComponentState state = (DMISpriteComponentState)curState;
            AppearanceId = state.AppearanceId;

            //TODO: Load appearance
        }

        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null) {
            //Vector2 position = (worldPos ?? Vector2.Zero) + (0.5f, 0.5f);
            ////TODO: Unit size is likely stored somewhere, use that instead of hardcoding 32
            //Vector2 size = (DMI?.IconSize ?? Vector2.Zero) / (32, 32) / 2;

            //return new Box2(position, position + size);
            return new Box2(0, 0, 0, 0); //TODO
        }

        public bool IsMouseOver(Vector2 position) {
            //TODO: mouse_opacity
            return true;
        }
    }
}
