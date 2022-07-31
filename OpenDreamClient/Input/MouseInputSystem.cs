using OpenDreamClient.Input.ContextMenu;
using OpenDreamShared.Input;
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
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

        private ContextMenuPopup _contextMenu;

        public override void Initialize() {
            _contextMenu = new ContextMenuPopup();
            _userInterfaceManager.ModalRoot.AddChild(_contextMenu);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, outsidePrediction: true))
                .Bind(EngineKeyFunctions.UIRightClick, new PointerInputCmdHandler(OnRightClick, outsidePrediction: true))
                .Register<MouseInputSystem>();
        }

        public override void Shutdown() {
            CommandBinds.Unregister<MouseInputSystem>();
        }

        private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args) {
            bool shift = _inputManager.IsKeyDown(Keyboard.Key.Shift);
            bool ctrl = _inputManager.IsKeyDown(Keyboard.Key.Control);
            bool alt = _inputManager.IsKeyDown(Keyboard.Key.Alt);

            if (args.EntityUid == EntityUid.Invalid) { // Turf was clicked
                EntityUid? gridUid = args.Coordinates.GetGridUid(_entityManager);
                if (gridUid == null)
                    return false;

                IMapGrid grid = _mapManager.GetGrid(gridUid.Value);
                Vector2i position = grid.CoordinatesToTile(args.Coordinates);
                RaiseNetworkEvent(new TurfClickedEvent(position, (int)grid.ParentMapId, shift, ctrl, alt));
                return true;
            }

            RaiseNetworkEvent(new EntityClickedEvent(args.EntityUid, shift, ctrl, alt));
            return true;
        }

        private bool OnRightClick(in PointerInputCmdHandler.PointerInputCmdArgs args) {
            if (args.EntityUid == EntityUid.Invalid)
                return false;

            _contextMenu.RepopulateEntities(_lookupSystem.GetEntitiesInRange(args.Coordinates, 0.01f));
            _contextMenu.Measure(_userInterfaceManager.ModalRoot.Size);
            _contextMenu.Open(UIBox2.FromDimensions(args.ScreenCoordinates.Position, _contextMenu.DesiredSize));

            return true;
        }
    }
}
