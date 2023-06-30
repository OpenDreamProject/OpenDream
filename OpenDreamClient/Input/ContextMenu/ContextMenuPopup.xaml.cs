using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;

namespace OpenDreamClient.Input.ContextMenu {
    [GenerateTypedNameReferences]
    public sealed partial class ContextMenuPopup : Popup {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        private readonly TransformSystem? _transformSystem;

        public int EntityCount => ContextMenu.ChildCount;

        public ContextMenuPopup() {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            _entitySystemManager.TryGetEntitySystem(out _transformSystem);
        }

        public void RepopulateEntities(IEnumerable<EntityUid> entities) {
            ContextMenu.RemoveAllChildren();

            if (_transformSystem == null)
                return;

            foreach (EntityUid entity in entities) {
                if (!_mapManager.IsGrid(_transformSystem.GetParent(entity).Owner)) // Not a child of another entity
                    continue;
                if (!_entityManager.TryGetComponent(entity, out DMISpriteComponent? sprite)) // Has a sprite
                    continue;
                if (sprite.Icon.Appearance.MouseOpacity == MouseOpacity.Transparent) // Not transparent to mouse clicks
                    continue;

                ContextMenu.AddChild(new ContextMenuItem(_uiManager, _entityManager, entity));
            }
        }
    }
}
