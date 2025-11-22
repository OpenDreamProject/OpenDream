using OpenDreamClient.Input.ContextMenu;
using OpenDreamClient.Interface;
using OpenDreamClient.Interface.Controls.UI;
using OpenDreamShared.Interface.Descriptors;
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
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IDreamInterfaceManager _dreamInterfaceManager = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;

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

    public void HandleAtomMouseEntered(ScalingViewport viewport, Vector2 relativePos, ClientObjectReference atomRef, Vector2i iconPos) {
        if (!HasMouseEventEnabled(atomRef, AtomMouseEvents.Enter))
            return;

        RaiseNetworkEvent(new MouseEnteredEvent(atomRef, CreateClickParams(viewport, relativePos, iconPos)));
    }

    public void HandleAtomMouseExited(ScalingViewport viewport, ClientObjectReference atomRef) {
        if (!HasMouseEventEnabled(atomRef, AtomMouseEvents.Exit))
            return;

        RaiseNetworkEvent(new MouseExitedEvent(atomRef, CreateClickParams(viewport, Vector2.Zero, Vector2i.Zero)));
    }

    public void HandleAtomMouseMove(ScalingViewport viewport, Vector2 relativePos, ClientObjectReference atomRef, Vector2i iconPos) {
        if (!HasMouseEventEnabled(atomRef, AtomMouseEvents.Move))
            return;

        RaiseNetworkEvent(new MouseMoveEvent(atomRef, CreateClickParams(viewport, relativePos, iconPos)));
    }

    public (ClientObjectReference Atom, Vector2i IconPosition, bool IsScreen)? GetAtomUnderMouse(ScalingViewport viewport, Vector2 relativePos, ScreenCoordinates globalPos) {
        _dreamViewOverlay ??= _overlayManager.GetOverlay<DreamViewOverlay>();
        if(_dreamViewOverlay.MouseMap == null)
            return null;

        var viewportBox = viewport.GetDrawBox();
        if (!viewportBox.Contains((int)relativePos.X, (int)relativePos.Y))
            return null; // Was outside of the viewport

        var mapCoords = viewport.ScreenToMap(globalPos.Position);
        var mousePos = (relativePos - viewportBox.TopLeft) / viewportBox.Size * viewport.ViewportSize;
        if (mousePos.X >= _dreamViewOverlay.MouseMap.Size.X || mousePos.Y >= _dreamViewOverlay.MouseMap.Size.Y)
            return null;

        if(_configurationManager.GetCVar(CVars.DisplayCompat))
            return null; //Compat mode causes crashes with RT's GetPixel because OpenGL ES doesn't support GetTexImage()
        var lookupColor = _dreamViewOverlay.MouseMap.GetPixel((int)mousePos.X, (int)mousePos.Y);
        var underMouse = _dreamViewOverlay.MouseMapLookup.GetValueOrDefault(lookupColor);
        if (underMouse == null)
            return null;

        if (underMouse.ClickUid == EntityUid.Invalid) { // A turf
            var turf = GetTurfUnderMouse(mapCoords, out _);
            if (turf == null)
                return null;

            return (turf.Value.Atom, turf.Value.IconPosition, false);
        } else {
            var iconPosition = (Vector2i) ((mapCoords.Position - underMouse.Position) * _dreamInterfaceManager.IconSize);
            var reference = new ClientObjectReference(_entityManager.GetNetEntity(underMouse.ClickUid));

            return (reference, iconPosition, underMouse.IsScreen);
        }
    }

    public (ClientObjectReference Atom, Vector2i IconPosition)? GetTurfUnderMouse(MapCoordinates mapCoords, out uint? turfId) {
        // Grid coordinates are half a meter off from entity coordinates
        mapCoords = new MapCoordinates(mapCoords.Position + new Vector2(0.5f), mapCoords.MapId);

        if (_mapManager.TryFindGridAt(mapCoords, out var gridEntity, out var grid)) {
            Vector2i position = _mapSystem.CoordinatesToTile(gridEntity, grid, _mapSystem.MapToGrid(gridEntity, mapCoords));
            _mapSystem.TryGetTile(grid, position, out Tile tile);
            turfId = (uint)tile.TypeId;
            Vector2i turfIconPosition = (Vector2i) ((mapCoords.Position - position) * _dreamInterfaceManager.IconSize);
            MapCoordinates worldPosition = _mapSystem.GridTileToWorld(gridEntity, grid, position);

            return (new(position, (int)worldPosition.MapId), turfIconPosition);
        }

        turfId = null;
        return null;
    }

    private bool OnPress(ScalingViewport viewport, GUIBoundKeyEventArgs args, ControlDescriptor descriptor) {
        //either turf or atom was clicked, and it was a right-click, and the popup menu is enabled, and the right-click parameter is disabled
        if (args.Function == EngineKeyFunctions.UIRightClick && _dreamInterfaceManager.ShowPopupMenus && !descriptor.RightClick.Value) {
            _contextMenu.RepopulateEntities(viewport, args.RelativePosition, args.PointerLocation);
            if (_contextMenu.EntityCount != 0) { //don't open a 1x1 empty context menu
                var contextMenuLocation = args.PointerLocation.Position / _userInterfaceManager.ModalRoot.UIScale; // Take scaling into account

                _contextMenu.Measure(_userInterfaceManager.ModalRoot.Size);
                _contextMenu.Open(UIBox2.FromDimensions(contextMenuLocation, _contextMenu.DesiredSize));
            }

            return true;
        }

        var underMouse = GetAtomUnderMouse(viewport, args.RelativePixelPosition, args.PointerLocation);
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
            var overAtom = GetAtomUnderMouse(viewport, args.RelativePixelPosition, args.PointerLocation);

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

    /// <summary>
    /// <see cref="CreateClickParams(OpenDreamClient.Interface.Controls.UI.ScalingViewport,Robust.Client.UserInterface.GUIBoundKeyEventArgs,Robust.Shared.Maths.Vector2i)"/>
    /// but without information about mouse/keyboard buttons
    /// </summary>
    private ClickParams CreateClickParams(ScalingViewport viewport, Vector2 relativePos, Vector2i iconPos) {
        UIBox2i viewportBox = viewport.GetDrawBox();
        Vector2 screenLocPos = (relativePos - viewportBox.TopLeft) / viewportBox.Size * viewport.ViewportSize;
        float screenLocY = viewport.ViewportSize.Y - screenLocPos.Y; // Flip the Y
        ScreenLocation screenLoc = new ScreenLocation((int) screenLocPos.X, (int) screenLocY, 32); // TODO: icon_size other than 32

        // TODO: Take icon transformations into account for iconPos
        return new(screenLoc, false, false, false, false, false, iconPos.X, iconPos.Y);
    }

    private bool HasMouseEventEnabled(ClientObjectReference atomRef, AtomMouseEvents mouseEvent) {
        if (!_appearanceSystem.TryGetAppearance(atomRef, out var appearance))
            return false;

        return appearance.EnabledMouseEvents.HasFlag(mouseEvent);
    }
}
