using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;

namespace OpenDreamClient.Input.ContextMenu;

[GenerateTypedNameReferences]
internal sealed partial class ContextMenuPopup : Popup {
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    private readonly ClientAppearanceSystem? _appearanceSystem;
    private readonly TransformSystem? _transformSystem;
    private readonly ClientVerbSystem? _verbSystem;
    private readonly EntityQuery<DMISpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<DreamMobSightComponent> _mobSightQuery;

    public int EntityCount => ContextMenu.ChildCount;

    private VerbMenuPopup? _currentVerbMenu;

    public ContextMenuPopup() {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _entitySystemManager.TryGetEntitySystem(out _transformSystem);
        _entitySystemManager.TryGetEntitySystem(out _verbSystem);
        _entitySystemManager.TryGetEntitySystem(out _appearanceSystem);
        _spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        _xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        _mobSightQuery = _entityManager.GetEntityQuery<DreamMobSightComponent>();
    }

    public void RepopulateEntities(ClientObjectReference[] entities, uint? turfId) {
        ContextMenu.RemoveAllChildren();

        if (_transformSystem == null || _appearanceSystem == null)
            return;

        foreach (var objectReference in entities) {
            var name = _appearanceSystem.GetName(objectReference);
            DreamIcon? icon = null;

            switch (objectReference.Type) {
                case ClientObjectReference.RefType.Entity: {
                    var entity = _entityManager.GetEntity(objectReference.Entity);
                    if (_xformQuery.TryGetComponent(entity, out TransformComponent? transform) && !_mapManager.IsGrid(_transformSystem.GetParentUid(entity))) // Not a child of another entity
                        continue;
                    if (!_spriteQuery.TryGetComponent(entity, out DMISpriteComponent? sprite)) // Has a sprite
                        continue;
                    if (sprite.Icon.Appearance?.MouseOpacity == MouseOpacity.Transparent) // Not transparent to mouse clicks
                        continue;
                    if (!sprite.IsVisible(transform, GetSeeInvisible())) // Not invisible
                        continue;

                    icon = sprite.Icon;
                    break;
                }
                case ClientObjectReference.RefType.Turf when turfId is not null:
                    icon = _appearanceSystem.GetTurfIcon(turfId.Value);
                    break;
            }

            if (icon is null)
                continue;

            ContextMenu.AddChild(new ContextMenuItem(this, objectReference, name, icon));
        }
    }

    public void SetActiveItem(ContextMenuItem item) {
        if (_currentVerbMenu != null) {
            _currentVerbMenu.Close();
            _uiManager.ModalRoot.RemoveChild(_currentVerbMenu);
        }

        _currentVerbMenu = new VerbMenuPopup(_verbSystem, GetSeeInvisible(), item.Target);

        _currentVerbMenu.OnVerbSelected += Close;

        Vector2 desiredSize = _currentVerbMenu.DesiredSize;
        Vector2 verbMenuPos = item.GlobalPosition with { X = item.GlobalPosition.X + item.Size.X };
        _uiManager.ModalRoot.AddChild(_currentVerbMenu);
        _currentVerbMenu.Open(UIBox2.FromDimensions(verbMenuPos, desiredSize));
    }

    /// <returns>The see_invisible of our current mob</returns>
    private sbyte GetSeeInvisible() {
        if (_playerManager.LocalSession == null)
            return 127;
        if (!_mobSightQuery.TryGetComponent(_playerManager.LocalSession.AttachedEntity, out DreamMobSightComponent? sight))
            return 127;

        return sight.SeeInvisibility;
    }
}
