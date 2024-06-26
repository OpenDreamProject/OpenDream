﻿using OpenDreamShared.Dream;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectFilter(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public static readonly Dictionary<DreamFilter, DreamFilterList> FilterAttachedTo = new();

    public override bool ShouldCallNew => false;

    public DreamFilter Filter;

    protected override void HandleDeletion() {
        base.HandleDeletion();

        FilterAttachedTo.Remove(Filter);
    }

    // TODO: Variable getting

    protected override void SetVar(string varName, DreamValue value) {
        if (FilterAttachedTo.TryGetValue(Filter, out var attachedTo)) {
            int index = attachedTo.GetIndexOfFilter(Filter);
            Type filterType = Filter.GetType();

            // Create a new mapping with the modified value and replace the DreamFilter with it
            MappingDataNode mapping = (MappingDataNode)SerializationManager.WriteValue(filterType, Filter);
            mapping.Remove(varName);
            mapping.Add(varName, new DreamValueDataNode(value));
            if (SerializationManager.Read(filterType, mapping) is not DreamFilter newFilter)
                return;
            if (newFilter.Equals(Filter)) // No change
                return;

            Filter = newFilter;
            attachedTo.SetFilter(index, newFilter);
        }
    }

    public static DreamObjectFilter? TryCreateFilter(DreamObjectTree objectTree, IEnumerable<(string Name, DreamValue Value)> properties) {
        Type? filterType = null;
        MappingDataNode attributes = new();

        foreach (var property in properties) {
            if (property.Value.IsNull)
                continue;

            if (property.Name == "type" && property.Value.TryGetValueAsString(out var filterTypeName)) {
                filterType = DreamFilter.GetType(filterTypeName);
            }

            attributes.Add(property.Name, new DreamValueDataNode(property.Value));
        }

        if (filterType == null)
            return null;

        var serializationManager = IoCManager.Resolve<ISerializationManager>();

        DreamFilter? filter = serializationManager.Read(filterType, attributes) as DreamFilter;
        if (filter is null)
            throw new Exception($"Failed to create filter of type {filterType}");

        var filterObject = objectTree.CreateObject<DreamObjectFilter>(objectTree.Filter);
        filterObject.Filter = filter;
        return filterObject;
    }

    public static DreamObjectFilter? TryCreateFilter(DreamObjectTree objectTree, DreamList list) {
        static IEnumerable<(string, DreamValue)> EnumerateProperties(DreamList list) {
            foreach (var key in list.GetValues()) {
                if (!key.TryGetValueAsString(out var keyStr))
                    continue;

                var value = list.GetValue(key);
                if (value.IsNull)
                    continue;

                yield return (keyStr, value);
            }
        }

        return TryCreateFilter(objectTree, EnumerateProperties(list));
    }
}
