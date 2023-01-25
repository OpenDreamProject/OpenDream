using OpenDreamClient.Input.ContextMenu;
using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace OpenDreamClient.Input {
    sealed class MouseInputSystem : SharedMouseInputSystem {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

        private ContextMenuPopup _contextMenu;

        public override void Initialize() {
            _contextMenu = new ContextMenuPopup();
            _userInterfaceManager.ModalRoot.AddChild(_contextMenu);
        }

        public override void Shutdown() {
            CommandBinds.Unregister<MouseInputSystem>();
        }

        public bool HandleViewportClick(ScalingViewport viewport, GUIBoundKeyEventArgs args) {
            UIBox2i viewportBox = viewport.GetDrawBox();
            if (!viewportBox.Contains((int)args.RelativePixelPosition.X, (int)args.RelativePixelPosition.Y))
                return false; // Click was outside of the viewport

            bool shift = _inputManager.IsKeyDown(Keyboard.Key.Shift);
            bool ctrl = _inputManager.IsKeyDown(Keyboard.Key.Control);
            bool alt = _inputManager.IsKeyDown(Keyboard.Key.Alt);

            MapCoordinates mapCoords = viewport.ScreenToMap(args.PointerLocation.Position);
            EntityUid entity = GetEntityUnderMouse(viewport, args.PointerLocation.Position, mapCoords);

            if (args.Function == EngineKeyFunctions.UIRightClick) {
                if (entity == EntityUid.Invalid)
                    return false;

                _contextMenu.RepopulateEntities(_lookupSystem.GetEntitiesInRange(mapCoords, 0.01f));
                _contextMenu.Measure(_userInterfaceManager.ModalRoot.Size);
                Vector2 contextMenuLocation = args.PointerLocation.Position / _userInterfaceManager.ModalRoot.UIScale; // Take scaling into account
                _contextMenu.Open(UIBox2.FromDimensions(contextMenuLocation, _contextMenu.DesiredSize));

                return true;
            }

            Vector2 screenLocPos = (args.RelativePixelPosition - viewportBox.TopLeft) / viewportBox.Size;
            screenLocPos *= viewport.ViewportSize;
            screenLocPos.Y = viewport.ViewportSize.Y - screenLocPos.Y; // Flip the Y
            ScreenLocation screenLoc = new ScreenLocation((int) screenLocPos.X, (int) screenLocPos.Y, 32); // TODO: icon_size other than 32

            if (entity == EntityUid.Invalid) { // Turf was clicked
                if (!_mapManager.TryFindGridAt(mapCoords, out var grid))
                    return false;

                Vector2i position = grid.CoordinatesToTile(mapCoords);
                MapCoordinates worldPosition = grid.GridTileToWorld(position);
                RaiseNetworkEvent(new TurfClickedEvent(position, (int)worldPosition.MapId, screenLoc,  shift, ctrl, alt));
                return true;
            }

            RaiseNetworkEvent(new EntityClickedEvent(entity, screenLoc, shift, ctrl, alt));
            return true;
        }

        private EntityUid GetEntityUnderMouse(ScalingViewport viewport, Vector2 mousePos, MapCoordinates coords) {
            EntityUid? entity = GetEntityOnScreen(mousePos, viewport);
            entity ??= GetEntityOnMap(coords);

            return entity ?? EntityUid.Invalid;
        }

        private EntityUid? GetEntityOnScreen(Vector2 mousePos, ScalingViewport viewport) {
            ClientScreenOverlaySystem screenOverlay = EntitySystem.Get<ClientScreenOverlaySystem>();
            EntityUid? eye = _playerManager.LocalPlayer.Session.AttachedEntity;
            if (eye == null || !_entityManager.TryGetComponent<TransformComponent>(eye.Value, out var eyeTransform)) {
                return null;
            }

            Vector2 viewOffset = eyeTransform.WorldPosition - 7.5f; //TODO: Don't hardcode a 15x15 view
            MapCoordinates coords = viewport.ScreenToMap(mousePos);

            var foundSprites = new List<(DreamIcon, Vector2, EntityUid, Boolean)>();
            foreach (DMISpriteComponent sprite in screenOverlay.EnumerateScreenObjects()) {
                Vector2 position = sprite.ScreenLocation.GetViewPosition(viewOffset, EyeManager.PixelsPerMeter);

                if (sprite.CheckClickScreen(position, coords.Position)) {
                    foundSprites.Add((sprite.Icon, position, sprite.Owner, true));
                }
            }

            if (foundSprites.Count == 0)
                return null;

            //foundSprites.Sort(new RenderOrderComparer());
            return foundSprites[^1].Item3;
        }

        private EntityUid? GetEntityOnMap(MapCoordinates coords) {
            IEnumerable<EntityUid> entities = _lookupSystem.GetEntitiesIntersecting(coords.MapId, Box2.CenteredAround(coords.Position, (0.1f, 0.1f)));

            var foundSprites = new List<(DreamIcon, Vector2, EntityUid, Boolean)>();
            foreach (EntityUid entity in entities) {
                if (_entityManager.TryGetComponent<DMISpriteComponent>(entity, out var sprite)
                    && sprite.CheckClickWorld(coords.Position)) {
                    foundSprites.Add((sprite.Icon, coords.Position, sprite.Owner, false));
                }
            }

            if (foundSprites.Count == 0)
                return null;

            //foundSprites.Sort(new RenderOrderComparer());
            return foundSprites[^1].Item3;
        }
    }
}
