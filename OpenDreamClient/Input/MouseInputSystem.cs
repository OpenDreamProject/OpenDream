using OpenDreamClient.Input.ContextMenu;
using OpenDreamClient.Interface;
using OpenDreamClient.Interface.Controls.UI;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace OpenDreamClient.Input;

internal sealed class MouseInputSystem : SharedMouseInputSystem {
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IDreamInterfaceManager _dreamInterfaceManager = default!;

    private DreamViewOverlay? _dreamViewOverlay;
    private ContextMenuPopup _contextMenu = default!;
    private EntityClickInformation? _selectedEntity;

    private sealed class EntityClickInformation(ClientObjectReference atom, ScreenCoordinates initialMousePos, ClickParams clickParams) {
        public readonly ClientObjectReference Atom = atom;
        public readonly ScreenCoordinates InitialMousePos = initialMousePos;
        public readonly ClickParams ClickParams = clickParams;
        public bool IsDrag; // If the current click is considered a drag (if the mouse has moved after the click)
    }

    public override void Initialize() {
        UpdatesOutsidePrediction = true;

        _contextMenu = new ContextMenuPopup();
        _userInterfaceManager.ModalRoot.AddChild(_contextMenu);
    }

    public override void Update(float frameTime) {
        if (_selectedEntity == null)
            return;

        if (!_selectedEntity.IsDrag) {
            var currentMousePos = _inputManager.MouseScreenPosition.Position;
            var distance = (currentMousePos - _selectedEntity.InitialMousePos.Position).Length();

            if (distance > 3f)
                _selectedEntity.IsDrag = true;
        }
    }

    public override void Shutdown() {
        CommandBinds.Unregister<MouseInputSystem>();
    }

    public bool HandleViewportEvent(ScalingViewport viewport, GUIBoundKeyEventArgs args, ControlDescriptor descriptor) {
        if (args.State == BoundKeyState.Down)
            return OnPress(viewport, args, descriptor);
        else
            return OnRelease(viewport, args);
    }

    public void HandleStatClick(string atomRef, bool isRight, bool isMiddle) {
        bool shift = _inputManager.IsKeyDown(Keyboard.Key.Shift);
        bool ctrl = _inputManager.IsKeyDown(Keyboard.Key.Control);
        bool alt = _inputManager.IsKeyDown(Keyboard.Key.Alt);

        RaiseNetworkEvent(new StatClickedEvent(atomRef, isRight, isMiddle, shift, ctrl, alt));
    }

    private (ClientObjectReference Atom, Vector2i IconPosition)? GetAtomUnderMouse(ScalingViewport viewport, GUIBoundKeyEventArgs args) {
        _dreamViewOverlay ??= _overlayManager.GetOverlay<DreamViewOverlay>();
        if(_dreamViewOverlay.MouseMap == null)
            return null;

        UIBox2i viewportBox = viewport.GetDrawBox();
        if (!viewportBox.Contains((int)args.RelativePixelPosition.X, (int)args.RelativePixelPosition.Y))
            return null; // Was outside of the viewport

        var mapCoords = viewport.ScreenToMap(args.PointerLocation.Position);
        var mousePos = (args.RelativePixelPosition - viewportBox.TopLeft) / viewportBox.Size * viewport.ViewportSize;

        if(_configurationManager.GetCVar(CVars.DisplayCompat))
            return null; //Compat mode causes crashes with RT's GetPixel because OpenGL ES doesn't support GetTexImage()
        var lookupColor = _dreamViewOverlay.MouseMap.GetPixel((int)mousePos.X, (int)mousePos.Y);
        var underMouse = _dreamViewOverlay.MouseMapLookup.GetValueOrDefault(lookupColor);
        if (underMouse == null)
            return null;

        if (underMouse.ClickUid == EntityUid.Invalid) { // A turf
            return GetTurfUnderMouse(mapCoords);
        } else {
            Vector2i iconPosition = (Vector2i) ((mapCoords.Position - underMouse.Position) * EyeManager.PixelsPerMeter);

            return (new(_entityManager.GetNetEntity(underMouse.ClickUid)), iconPosition);
        }
    }

    private (ClientObjectReference Atom, Vector2i IconPosition)? GetTurfUnderMouse(MapCoordinates mapCoords) {
        // Grid coordinates are half a meter off from entity coordinates
        mapCoords = new MapCoordinates(mapCoords.Position + new Vector2(0.5f), mapCoords.MapId);

        if (_mapManager.TryFindGridAt(mapCoords, out var gridEntity, out var grid)) {
            Vector2i position = _mapSystem.CoordinatesToTile(gridEntity, grid, _mapSystem.MapToGrid(gridEntity, mapCoords));
            Vector2i turfIconPosition = (Vector2i) ((mapCoords.Position - position) * EyeManager.PixelsPerMeter);
            MapCoordinates worldPosition = _mapSystem.GridTileToWorld(gridEntity, grid, position);

            return (new(position, (int)worldPosition.MapId), turfIconPosition);
        }

        return null;
    }

    private bool OnPress(ScalingViewport viewport, GUIBoundKeyEventArgs args, ControlDescriptor descriptor) {
        //either turf or atom was clicked, and it was a right-click, and the popup menu is enabled, and the right-click parameter is disabled
        if (args.Function == EngineKeyFunctions.UIRightClick && _dreamInterfaceManager.ShowPopupMenus && !descriptor.RightClick.Value) {
            var mapCoords = viewport.ScreenToMap(args.PointerLocation.Position);
            var entities = _lookupSystem.GetEntitiesInRange(mapCoords, 0.01f, LookupFlags.Uncontained | LookupFlags.Approximate);

            ClientObjectReference[] objects = new ClientObjectReference[entities.Count + 1];

            // We can't index a HashSet so we have to use a foreach loop
            int index = 0;
            foreach (var uid in entities) {
                objects[index] = new ClientObjectReference(_entityManager.GetNetEntity(uid));
                index += 1;
            }

            // Append the turf to the end of the context menu
            var turfUnderMouse = GetTurfUnderMouse(mapCoords)?.Atom;
            if (turfUnderMouse is not null)
                objects[index] = turfUnderMouse.Value;

            //TODO filter entities by the valid verbs that exist on them
            //they should only show up if there is a verb attached to usr which matches the filter in world syntax
            //ie, obj|turf in world
            //note that popup_menu = 0 overrides this behaviour, as does verb invisibility (urgh), and also hidden
            //because BYOND sure loves redundancy

            _contextMenu.RepopulateEntities(objects);
            if(_contextMenu.EntityCount == 0)
                return true; //don't open a 1x1 empty context menu

            _contextMenu.Measure(_userInterfaceManager.ModalRoot.Size);
            Vector2 contextMenuLocation = args.PointerLocation.Position / _userInterfaceManager.ModalRoot.UIScale; // Take scaling into account
            _contextMenu.Open(UIBox2.FromDimensions(contextMenuLocation, _contextMenu.DesiredSize));

            return true;
        }

        var underMouse = GetAtomUnderMouse(viewport, args);
        if (underMouse == null)
            return false;

        var atom = underMouse.Value.Atom;
        var clickParams = CreateClickParams(viewport, args, underMouse.Value.IconPosition); // If client.show_popup_menu is disabled, this will handle sending right clicks

        _selectedEntity = new(atom, args.PointerLocation, clickParams);
        return true;
    }

    private bool OnRelease(ScalingViewport viewport, GUIBoundKeyEventArgs args) {
        if (_selectedEntity == null)
            return false;

        if (!_selectedEntity.IsDrag) {
            RaiseNetworkEvent(new AtomClickedEvent(_selectedEntity.Atom, _selectedEntity.ClickParams));
        } else {
            var overAtom = GetAtomUnderMouse(viewport, args);

            RaiseNetworkEvent(new AtomDraggedEvent(_selectedEntity.Atom, overAtom?.Atom, _selectedEntity.ClickParams));
        }

        _selectedEntity = null;
        return true;
    }

    private ClickParams CreateClickParams(ScalingViewport viewport, GUIBoundKeyEventArgs args, Vector2i iconPos) {
        bool right = args.Function == EngineKeyFunctions.UIRightClick;
        bool middle = args.Function == OpenDreamKeyFunctions.MouseMiddle;
        bool shift = _inputManager.IsKeyDown(Keyboard.Key.Shift);
        bool ctrl = _inputManager.IsKeyDown(Keyboard.Key.Control);
        bool alt = _inputManager.IsKeyDown(Keyboard.Key.Alt);
        UIBox2i viewportBox = viewport.GetDrawBox();
        Vector2 screenLocPos = (args.RelativePixelPosition - viewportBox.TopLeft) / viewportBox.Size * viewport.ViewportSize;
        float screenLocY = viewport.ViewportSize.Y - screenLocPos.Y; // Flip the Y
        ScreenLocation screenLoc = new ScreenLocation((int) screenLocPos.X, (int) screenLocY, 32); // TODO: icon_size other than 32

        // TODO: Take icon transformations into account for iconPos
        return new(screenLoc, right, middle, shift, ctrl, alt, iconPos.X, iconPos.Y);
    }
}
