using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Client.Graphics;
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

        public DMISpriteComponent() {
            Icon.SizeChanged += OnIconSizeChanged;
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DMISpriteComponentState state = (DMISpriteComponentState)curState;

            ScreenLocation = state.ScreenLocation;
            Icon.SetAppearance(state.AppearanceId);
        }

        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null) {
            return Icon.GetWorldAABB(worldPos);
        }

        public bool IsVisible(bool checkWorld = true) {
            if (Icon == null) return false;
            if (Icon.Appearance.Invisibility > 0) return false; //TODO: mob.see_invisibility

            if (checkWorld) {
                //Only render turfs (children of map entity) and their contents (secondary child of map entity)
                //TODO: Use RobustToolbox's container system/components?
                TransformComponent transform = Owner.Transform;
                EntityUid mapEntity = IoCManager.Resolve<IMapManager>().GetMapEntityId(transform.MapID);
                if (transform.ParentUid != mapEntity && transform.Parent?.ParentUid != mapEntity)
                    return false;
            }

            return true;
        }

        public bool CheckClickWorld(Vector2 worldPos) {
            if (!IsVisible()) return false;

            switch (Icon.Appearance.MouseOpacity) {
                case MouseOpacity.Opaque: return true;
                case MouseOpacity.Transparent: return false;
                case MouseOpacity.PixelOpaque: {
                    Vector2 iconPos = Owner.Transform.WorldPosition;

                    return Icon.CheckClickWorld(iconPos, worldPos);
                }
                default: throw new InvalidOperationException("Invalid mouse_opacity");
            }
        }

        public bool CheckClickScreen(Vector2 screenPos, Vector2 mousePos) {
            if (!IsVisible(checkWorld: false)) return false;

            switch (Icon.Appearance.MouseOpacity) {
                case MouseOpacity.Opaque: return Box2.FromDimensions(screenPos, Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter).Contains(mousePos);
                case MouseOpacity.Transparent: return false;
                case MouseOpacity.PixelOpaque: return Icon.CheckClickScreen(screenPos, mousePos);
                default: throw new InvalidOperationException("Invalid mouse_opacity");
            }
        }

        private void OnIconSizeChanged() {
            //Changing the icon's size leads to a new AABB used for entity lookups
            //These AABBs are cached, and have to be queued for an update
            EntitySystem.Get<DreamClientSystem>().QueueLookupTreeUpdate(Owner);
        }
    }
}
