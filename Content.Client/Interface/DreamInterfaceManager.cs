using Content.Client.Input;
using Content.Client.Resources;
using Content.Shared.Interface;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Client.Interface {
    class DreamInterfaceManager : IDreamInterfaceManager {
        [Dependency] private IResourceCache _resourceCache = default!;
        [Dependency] private IDreamMacroManager _macroManager = default!;

        public InterfaceDescriptor InterfaceDescriptor { get; private set; }

        public void LoadDMF(ResourcePath dmfPath) {
            if (!_resourceCache.TryGetResource(dmfPath, out DMFResource dmf) || dmf.Interface == null) {
                Logger.Error($"Error(s) while loading DMF '{dmfPath}'");

                return;
            }

            InterfaceDescriptor = dmf.Interface;
            _macroManager.LoadMacroSets(InterfaceDescriptor.MacroSetDescriptors);
            _macroManager.SetActiveMacroSet(InterfaceDescriptor.MacroSetDescriptors[0]);
        }
    }

    interface IDreamInterfaceManager {
        public InterfaceDescriptor InterfaceDescriptor { get; }

        public void LoadDMF(ResourcePath dmfPath);
    }
}
