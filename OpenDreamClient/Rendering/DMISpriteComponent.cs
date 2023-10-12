using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    internal sealed partial class DMISpriteComponent : SharedDMISpriteComponent {
        [ViewVariables] public DreamIcon Icon { get; set; } = new DreamIcon();
        [ViewVariables] public ScreenLocation? ScreenLocation { get; set; }

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemMan = default!;
        private EntityLookupSystem? _lookupSystem;

        public DMISpriteComponent() {
            Icon.SizeChanged += OnIconSizeChanged;
        }

        public bool IsVisible(bool checkWorld = true, int seeInvis = 0) {
            if (Icon.Appearance?.Invisibility > seeInvis) return false;

            if (checkWorld) {
                //Only render movables not inside another movable's contents (parented to the grid)
                //TODO: Use RobustToolbox's container system/components?
                if (!_entityManager.TryGetComponent<TransformComponent>(Owner, out var transform))
                    return false;

                if (transform.ParentUid != transform.GridUid)
                    return false;
            }

            return true;
        }

        private void OnIconSizeChanged() {
            _entityManager.TryGetComponent<TransformComponent>(Owner, out var transform);
            _lookupSystem ??= _entitySystemMan.GetEntitySystem<EntityLookupSystem>();
            _lookupSystem?.FindAndAddToEntityTree(Owner, xform: transform);
        }
    }
}
