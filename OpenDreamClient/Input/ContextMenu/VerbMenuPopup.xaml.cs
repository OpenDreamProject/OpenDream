using System.Linq;
using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.DebugWindows;
using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.ViewVariables;
using Robust.Shared.Map;

namespace OpenDreamClient.Input.ContextMenu;

[GenerateTypedNameReferences]
internal sealed partial class VerbMenuPopup : Popup {
    public delegate void VerbSelectedHandler();

    public VerbSelectedHandler? OnVerbSelected;

    private readonly ClientVerbSystem? _verbSystem;

    private readonly ClientObjectReference _target;

    public VerbMenuPopup(ClientVerbSystem? verbSystem, sbyte seeInvisible, ClientObjectReference target) {
        RobustXamlLoader.Load(this);

        _verbSystem = verbSystem;
        _target = target;

        if (verbSystem != null) {
            var sorted = verbSystem.GetExecutableVerbs(_target).Order(VerbNameComparer.OrdinalInstance);

            foreach (var (verbId, verbSrc, verbInfo) in sorted) {
                if (verbInfo.IsHidden(false, seeInvisible))
                    continue;
                if(!verbInfo.ShowInPopupAttribute)
                    continue;

                AddVerb(verbId, verbSrc, verbInfo);
            }
        }

#if TOOLS
        // We add some additional debugging tools in TOOLS mode
        var iconDebugButton = AddButton("Debug Icon");

        iconDebugButton.OnPressed += _ => {
            DreamIcon icon;
            switch (_target.Type) {
                case ClientObjectReference.RefType.Entity:
                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    var entityId = entityManager.GetEntity(_target.Entity);
                    if (!entityManager.TryGetComponent(entityId, out DMISpriteComponent? spriteComponent)) {
                        Logger.GetSawmill("opendream")
                            .Error($"Failed to get sprite component for {entityId} when trying to debug its icon");
                        return;
                    }

                    icon = spriteComponent.Icon;
                    break;
                case ClientObjectReference.RefType.Turf:
                    var mapManager = IoCManager.Resolve<IMapManager>();
                    var mapId = new MapId(_target.TurfZ);
                    var mapPos = new Vector2(_target.TurfX - 1, _target.TurfY - 1);
                    if (!mapManager.TryFindGridAt(mapId, mapPos, out var gridUid, out var grid)) {
                        Logger.GetSawmill("opendream")
                            .Error($"Failed to get icon for {_target} when trying to debug its icon");
                        return;
                    }

                    var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
                    var mapSystem = entitySystemManager.GetEntitySystem<MapSystem>();
                    var appearanceSystem = entitySystemManager.GetEntitySystem<ClientAppearanceSystem>();
                    var tileRef = mapSystem.GetTileRef(gridUid, grid, (Vector2i)mapPos);
                    icon = appearanceSystem.GetTurfIcon((uint)tileRef.Tile.TypeId);
                    break;
                default:
                    return;
            }

            new IconDebugWindow(icon).Show();
        };

        // If this is an entity, provide the option to use RT's VV
        if (_target.Type == ClientObjectReference.RefType.Entity) {
            var viewVariablesButton = AddButton("RT ViewVariables");

            viewVariablesButton.OnPressed += _ => {
                IoCManager.Resolve<IClientViewVariablesManager>().OpenVV(_target.Entity);
            };
        }
#endif
    }

    private void AddVerb(int verbId, ClientObjectReference verbSrc, VerbSystem.VerbInfo verbInfo) {
        var button = AddButton(verbInfo.Name);
        var takesTargetArg = verbInfo.GetTargetType() != null && !verbSrc.Equals(_target);

        button.OnPressed += _ => {
            _verbSystem?.ExecuteVerb(verbSrc, verbId, takesTargetArg ? [_target] : []);
        };
    }

    private Button AddButton(string text) {
        var button = new Button {
            Text = text
        };

        button.OnPressed += _ => {
            Close();
            OnVerbSelected?.Invoke();
        };

        VerbMenu.AddChild(button);
        return button;
    }
}
