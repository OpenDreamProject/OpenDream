using OpenDreamClient.Input.ContextMenu;
using OpenDreamShared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace OpenDreamClient.Input {
    sealed class MouseInputSystem : SharedMouseInputSystem {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

        private ContextMenuPopup _contextMenu;

        public override void Initialize() {
            _contextMenu = new ContextMenuPopup();
            _userInterfaceManager.ModalRoot.AddChild(_contextMenu);

            CommandBinds.Builder.Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, outsidePrediction: true)).Register<MouseInputSystem>();
            CommandBinds.Builder.Bind(EngineKeyFunctions.UIRightClick, new PointerInputCmdHandler(OnRightClick, outsidePrediction: true)).Register<MouseInputSystem>();
        }

        private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args) {
            if (args.EntityUid == EntityUid.Invalid)
                return false;

            bool shift = _inputManager.IsKeyDown(Keyboard.Key.Shift);
            bool ctrl = _inputManager.IsKeyDown(Keyboard.Key.Control);
            bool alt = _inputManager.IsKeyDown(Keyboard.Key.Alt);
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
