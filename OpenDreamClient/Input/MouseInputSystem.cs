using OpenDreamShared.Input;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace OpenDreamClient.Input {
    sealed class MouseInputSystem : SharedMouseInputSystem {
        [Dependency] private readonly IInputManager _inputManager = default!;

        public override void Initialize() {
            CommandBinds.Builder.Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, outsidePrediction: true)).Register<MouseInputSystem>();
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
    }
}
