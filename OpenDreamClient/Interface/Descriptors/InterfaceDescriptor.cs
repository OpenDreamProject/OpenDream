﻿using System.Linq;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed class InterfaceDescriptor {
    public readonly List<WindowDescriptor> WindowDescriptors;
    public readonly List<MacroSetDescriptor> MacroSetDescriptors;
    public readonly List<MenuDescriptor> MenuDescriptors;

    public InterfaceDescriptor(List<WindowDescriptor> windowDescriptors, List<MacroSetDescriptor> macroSetDescriptors, List<MenuDescriptor> menuDescriptors) {
        WindowDescriptors = windowDescriptors;
        MacroSetDescriptors = macroSetDescriptors;
        MenuDescriptors = menuDescriptors;
    }

    public ElementDescriptor? GetElementDescriptor(string name) {
        return WindowDescriptors.Concat<ElementDescriptor>(MacroSetDescriptors)
            .Concat(MenuDescriptors).FirstOrDefault(descriptor => descriptor.Name == name);
    }
}

[Virtual, ImplicitDataDefinitionForInheritors]
public class ElementDescriptor {
    [DataField("type")]
    public string _type;

    [DataField("name")]
    protected string _name;

    public string Name {
        get => _name;
        init => _name = value;
    }

    public string Type {
        get => _type;
        protected init => _type = value;
    }

    public virtual ElementDescriptor? CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        throw new InvalidOperationException($"{this} cannot create a child descriptor");
    }

    public virtual ElementDescriptor CreateCopy(ISerializationManager serializationManager, string name) {
        throw new InvalidOperationException($"{this} cannot create a copy of itself");
    }

    public override string ToString() {
        return $"{GetType().Name}(Type={Type},Name={Name})";
    }
}
