using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;

namespace OpenDreamShared.Interface {
    public class InterfaceDescriptor {
        public List<WindowDescriptor> WindowDescriptors;
        public List<MacroSetDescriptor> MacroSetDescriptors;
        public Dictionary<string, MenuDescriptor> MenuDescriptors;

        public InterfaceDescriptor(List<WindowDescriptor> windowDescriptors, List<MacroSetDescriptor> macroSetDescriptors, Dictionary<string, MenuDescriptor> menuDescriptors) {
            WindowDescriptors = windowDescriptors;
            MacroSetDescriptors = macroSetDescriptors;
            MenuDescriptors = menuDescriptors;
        }
    }

    [ImplicitDataDefinitionForInheritors]
    public class ElementDescriptor {
        [DataField("name")]
        public string Name;
    }
}
