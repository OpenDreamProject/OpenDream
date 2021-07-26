using Content.Client.Input;
using Content.Client.Interface;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client {
    class DreamClientSystem : EntitySystem {
        [Dependency] private readonly IDreamMacroManager _macroManager = default!;
        [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;

        public override void Initialize() {
            IoCManager.InjectDependencies(this);

            SubscribeLocalEvent<PlayerAttachSysMessage>(OnPlayerAttached);
        }

        private void OnPlayerAttached(PlayerAttachSysMessage e) {
            // The active input context gets reset to "common" when a new player is attached
            // So we have to set it again
            _macroManager.SetActiveMacroSet(_interfaceManager.InterfaceDescriptor.MacroSetDescriptors[0]);
        }
    }
}
