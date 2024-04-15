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
            .Concat(MenuDescriptors).FirstOrDefault(descriptor => descriptor.Name.AsRaw() == name);
    }
}

[Virtual, ImplicitDataDefinitionForInheritors]
public partial class ElementDescriptor {
    [DataField("type")]
    public DMFPropertyString _type;

    [DataField("id")]
    protected DMFPropertyString _id;

    [DataField("name")]
    protected DMFPropertyString _name;

    public DMFPropertyString Id {
        get => _id.AsRaw().Equals("") ? _id = new DMFPropertyString(Guid.NewGuid().ToString()) : _id; //ensure unique ID for all elements. Empty ID elements aren't addressible anyway.
        init => _id = value;
    }

    public DMFPropertyString Name => _name ?? Id;

    public DMFPropertyString Type {
        get => _type;
        protected init => _type = value;
    }

    public virtual ElementDescriptor? CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        throw new InvalidOperationException($"{this} cannot create a child descriptor");
    }

    public virtual ElementDescriptor CreateCopy(ISerializationManager serializationManager, string id) {
        throw new InvalidOperationException($"{this} cannot create a copy of itself");
    }

    public override string ToString() {
        return $"{GetType().Name}(Type={Type},Name={Name})";
    }
}
