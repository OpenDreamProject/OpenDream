using JetBrains.Annotations;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    sealed class DMISpriteComponent : SharedDMISpriteComponent, ILookupWorldBox2Component {
        [ViewVariables] public DreamIcon Icon { get; set; } = new DreamIcon();
        [ViewVariables] public ScreenLocation ScreenLocation { get; set; } = null;

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemMan = default!;
        [CanBeNull] private EntityLookupSystem _lookupSystem;

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

        public Box2 GetAABB(Transform transform) {
            return Icon.GetWorldAABB(transform.Position);
        }

        public bool IsVisible(bool checkWorld = true, [CanBeNull] IMapManager mapManager = null) {
            if (Icon?.DMI == null) return false;
            if (Icon.Appearance.Invisibility > 0) return false; //TODO: mob.see_invisibility

            if (checkWorld) {
                //Only render turfs (children of map entity) and their contents (secondary child of map entity)
                //TODO: Use RobustToolbox's container system/components?
                if (!_entityManager.TryGetComponent<TransformComponent>(Owner, out var transform))
                    return false;

                IoCManager.Resolve(ref mapManager);
                EntityUid mapEntity = mapManager.GetMapEntityId(transform.MapID);
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
                    if (!_entityManager.TryGetComponent<TransformComponent>(Owner, out var transform))
                        return false;

                    return Icon.CheckClickWorld(transform.WorldPosition, worldPos);
                }
                default: throw new InvalidOperationException("Invalid mouse_opacity");
            }
        }

        public bool CheckClickScreen(Vector2 screenPos, Vector2 mousePos) {
            if (!IsVisible(checkWorld: false)) return false;
            if (Icon.Appearance.MouseOpacity == MouseOpacity.Transparent) return false;

            Vector2 size = Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter * (ScreenLocation.RepeatX, ScreenLocation.RepeatY);
            Box2 iconBox = Box2.FromDimensions(screenPos, size);
            if (!iconBox.Contains(mousePos)) return false;

            switch (Icon.Appearance.MouseOpacity) {
                case MouseOpacity.Opaque: return true;
                case MouseOpacity.PixelOpaque: return Icon.CheckClickScreen(screenPos, mousePos);
                default: throw new InvalidOperationException("Invalid mouse_opacity");
            }
        }

        private void OnIconSizeChanged() {
            _lookupSystem ??= _entitySystemMan.GetEntitySystem<EntityLookupSystem>();
            _lookupSystem?.UpdateBounds(Owner);
        }
    }
}
