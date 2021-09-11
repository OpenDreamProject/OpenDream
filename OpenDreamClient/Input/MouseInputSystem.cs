using OpenDreamShared.Input;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace OpenDreamClient.Input {
    class MouseInputSystem : SharedMouseInputSystem {
        public override void Initialize() {
            CommandBinds.Builder.Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse)).Register<MouseInputSystem>();
        }

        private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args) {
            if (args.EntityUid == EntityUid.Invalid)
                return false;

            RaiseNetworkEvent(new EntityClickedEvent(args.EntityUid));

            return true;
        }
    }
}
