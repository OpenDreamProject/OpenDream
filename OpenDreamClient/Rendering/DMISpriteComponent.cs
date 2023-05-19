using JetBrains.Annotations;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
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
        private EntityLookupSystem? _lookupSystem;

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

        public bool IsVisible(bool checkWorld = true, IMapManager? mapManager = null, int seeInvis = 0) {
            if (Icon.Appearance?.Invisibility > seeInvis) return false;

            if (checkWorld) {
                //Only render movables not inside another movable's contents (parented to the grid)
                //TODO: Use RobustToolbox's container system/components?
                if (!_entityManager.TryGetComponent<TransformComponent>(Owner, out var transform))
                    return false;

                IoCManager.Resolve(ref mapManager);
                if (transform.ParentUid != transform.GridUid)
                    return false;
            }

            return true;
        }

        private void OnIconSizeChanged() {
            _entityManager.TryGetComponent<TransformComponent>(Owner, out var transform);
            _lookupSystem ??= _entitySystemMan.GetEntitySystem<EntityLookupSystem>();
            _lookupSystem?.FindAndAddToEntityTree(Owner, transform);
        }
    }
}
