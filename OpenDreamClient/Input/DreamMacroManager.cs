using OpenDreamClient.Interface;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Input;

namespace OpenDreamClient.Input {
    sealed class DreamMacroManager : IDreamMacroManager {
        public Dictionary<string, InterfaceMacroSet> InterfaceMacroSets { get; } = new();

        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        public void LoadMacroSets(List<MacroSetDescriptor> macroSets) {
            InterfaceMacroSets.Clear();

            foreach (MacroSetDescriptor macroSet in macroSets) {
                LoadMacroSet(macroSet);
            }
        }

        public void LoadMacroSet(MacroSetDescriptor macroSetDescriptor) {
            InterfaceMacroSets.Add(macroSetDescriptor.Name, new(macroSetDescriptor, _entitySystemManager, _inputManager));
        }

        public void SetActiveMacroSet(string macroSetName) {
            if (!InterfaceMacroSets.TryGetValue(macroSetName, out var macroSet)) {
                Logger.Error($"Attempted to set active macro set to nonexistent set \"{macroSetName}\"");
                return;
            }

            macroSet.SetActive();
        }
    }

    interface IDreamMacroManager {
        public Dictionary<string, InterfaceMacroSet> InterfaceMacroSets { get; }

        public void LoadMacroSets(List<MacroSetDescriptor> macroSets);
        public void LoadMacroSet(MacroSetDescriptor macroSetDescriptor);
        public void SetActiveMacroSet(string macroSetName);
    }
}
