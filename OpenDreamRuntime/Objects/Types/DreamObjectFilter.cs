using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectFilter(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public override bool ShouldCallNew => false;

    public DreamFilter Filter;

    /// <summary>
    /// The filter list this filter was read from, or null for a standalone <c>filter()</c>.
    /// Writing a var updates that list. Adding the filter to another list stores a copy.
    /// </summary>
    public DreamFilterList? AttachedTo;

    protected override bool TryGetVar(string varName, out DreamValue value) {
        if (DreamFilterHelpers.TryGetVar(Filter, varName, ObjectTree, out value))
            return true;
        if (varName is "vars" or "parent_type" or "tag")
            return base.TryGetVar(varName, out value);

        throw new DMException($"Cannot get value of undefined var \"{varName}\" on {Filter.FilterType} filter");
    }

    protected override void SetVar(string varName, DreamValue value) {
        var newFilter = DreamFilterHelpers.SetVar(Filter, varName, value);
        if (ReferenceEquals(newFilter, Filter))
            return;

        AttachedTo?.ReplaceFilter(Filter, newFilter, varName);
        Filter = newFilter;
    }

    public static DreamObjectFilter? TryCreateFilter(DreamObjectTree objectTree, IEnumerable<(string Name, DreamValue Value)> properties) {
        Type? filterType = null;
        var propertyList = new List<(string Name, DreamValue Value)>();

        foreach (var property in properties) {
            if (property.Value.IsNull)
                continue;

            if (property.Name == "type" && property.Value.TryGetValueAsString(out var filterTypeName)) {
                filterType = DreamFilter.GetType(filterTypeName);
            }

            propertyList.Add(property);
        }

        if (filterType == null)
            return null;

        var filter = DreamFilterHelpers.Create(filterType, propertyList);

        var filterObject = objectTree.CreateObject<DreamObjectFilter>(objectTree.Filter);
        filterObject.Filter = filter;
        return filterObject;
    }

    public static DreamObjectFilter? TryCreateFilter(DreamObjectTree objectTree, DreamList list) {
        static IEnumerable<(string, DreamValue)> EnumerateProperties(DreamList list) {
            foreach (var key in list.EnumerateValues()) {
                if (!key.TryGetValueAsString(out var keyStr))
                    continue;

                using var value = list.GetValue(key);
                if (value.IsNull)
                    continue;

                yield return (keyStr, value);
            }
        }

        return TryCreateFilter(objectTree, EnumerateProperties(list));
    }
}
