using Content.Client.Input;
using Content.Client.Interface;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client {
    class DreamClientSystem : EntitySystem {
        [Dependency] private readonly IDreamMacroManager _macroManager = default!;
        [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
        [Dependency] private readonly ITimerManager _timerManager = default!;

        public override void Initialize() {
            IoCManager.InjectDependencies(this);

            SubscribeLocalEvent<PlayerAttachSysMessage>(OnPlayerAttached);
        }

        public override void FrameUpdate(float frameTime) {
            //TODO: These should run at the tick rate of the server, not the fps of the client
            _macroManager.RunActiveMacros();
        }

        private void OnPlayerAttached(PlayerAttachSysMessage e) {
            // The active input context gets reset to "common" when a new player is attached
            // So we have to set it again
            _macroManager.SetActiveMacroSet(_interfaceManager.InterfaceDescriptor.MacroSetDescriptors[0]);
        }
    }
}
