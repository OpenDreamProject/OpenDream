using OpenDreamClient.Input;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using System;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    class DMISpriteComponent : SharedDMISpriteComponent, ILookupWorldBox2Component {
        [ViewVariables] public DreamIcon Icon { get; set; } = new DreamIcon();
        [ViewVariables] public ScreenLocation ScreenLocation { get; set; } = null;

        [Dependency] private readonly IClickMapManager _clickMapManager = default!;

        public DMISpriteComponent() {
            Icon.DMIChanged += OnDMIChanged;
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DMISpriteComponentState state = (DMISpriteComponentState)curState;

            ScreenLocation = state.ScreenLocation;
            Icon.SetAppearance(state.AppearanceId);
        }

        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null) {
            //TODO: Unit size is likely stored somewhere, use that instead of hardcoding 32
            Vector2 size = (Icon?.DMI?.IconSize / (32, 32)) ?? Vector2.Zero;
            Vector2 pixelOffset = (Icon?.Appearance?.PixelOffset ?? Vector2.Zero) / (32, 32);
            Vector2 position = (worldPos ?? Vector2.Zero) + (size / 2) + pixelOffset;

            return Box2.CenteredAround(position, size);
        }

        public bool IsVisible() {
            if (Icon == null) return false;
            if (Icon.Appearance.Invisibility > 0) return false; //TODO: mob.see_invisibility

            //Only render turfs (children of map entity) and their contents (secondary child of map entity)
            //TODO: Use RobustToolbox's container system/components?
            ITransformComponent transform = Owner.Transform;
            EntityUid mapEntity = IoCManager.Resolve<IMapManager>().GetMapEntityId(transform.MapID);
            if (transform.ParentUid != mapEntity && transform.Parent?.ParentUid != mapEntity)
                return false;

            return true;
        }

        public bool CheckClick(Vector2 worldPos) {
            if (!IsVisible()) return false;

            switch (Icon.Appearance.MouseOpacity) {
                case MouseOpacity.Opaque: return true;
                case MouseOpacity.Transparent: return false;
                case MouseOpacity.PixelOpaque: {
                    if (Icon.CurrentFrame == null) return false;
                    Vector2 iconPos = GetWorldAABB(Owner.Transform.WorldPosition).BottomLeft;
                    Vector2 pos = (worldPos - iconPos) * Icon.DMI.IconSize;

                    return _clickMapManager.IsOccluding(Icon.CurrentFrame, ((int)pos.X, Icon.DMI.IconSize.Y - (int)pos.Y));
                }
                default: throw new InvalidOperationException("Invalid mouse_opacity");
            }
        }

        private void OnDMIChanged(DMIResource oldDMI, DMIResource newDMI) {
            //Changing the icon's size leads to a new AABB used for entity lookups
            //These AABBs are cached, and have to be queued for an update
            if (newDMI?.IconSize != oldDMI?.IconSize) {
                EntitySystem.Get<DreamClientSystem>().QueueLookupTreeUpdate(Owner);
            }
        }
    }
}
