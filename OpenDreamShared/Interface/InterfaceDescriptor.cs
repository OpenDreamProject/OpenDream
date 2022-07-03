using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization.Manager.Attributes;

namespace OpenDreamShared.Interface {
    public sealed class InterfaceDescriptor {
        public List<WindowDescriptor> WindowDescriptors;
        public List<MacroSetDescriptor> MacroSetDescriptors;
        public Dictionary<string, MenuDescriptor> MenuDescriptors;

        public InterfaceDescriptor(List<WindowDescriptor> windowDescriptors, List<MacroSetDescriptor> macroSetDescriptors, Dictionary<string, MenuDescriptor> menuDescriptors) {
            WindowDescriptors = windowDescriptors;
            MacroSetDescriptors = macroSetDescriptors;
            MenuDescriptors = menuDescriptors;
        }
    }

    [Virtual, ImplicitDataDefinitionForInheritors]
    public class ElementDescriptor {
        [DataField("name")]
        public string Name;
    }
}
