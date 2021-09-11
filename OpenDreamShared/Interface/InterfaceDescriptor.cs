using System;
using System.Collections.Generic;
using System.Reflection;

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

    public class ElementDescriptor {
        [InterfaceAttribute("name")]
        public string Name;

        private Dictionary<string, FieldInfo> _attributeNameToField = new();

        public ElementDescriptor(string name) {
            Name = name;

            foreach (FieldInfo field in GetType().GetFields()) {
                InterfaceAttributeAttribute attribute = field.GetCustomAttribute<InterfaceAttributeAttribute>();

                if (attribute != null) _attributeNameToField.Add(attribute.Name, field);
            }
        }

        public bool HasAttribute(string name) {
            return _attributeNameToField.ContainsKey(name);
        }

        public void SetAttribute(string name, object value) {
            FieldInfo field = _attributeNameToField[name];

            try {
                field.SetValue(this, value);
            } catch (ArgumentException) {
                throw new Exception("Cannot set attribute '" + name + "' to " + value);
            }
        }
    }
}
