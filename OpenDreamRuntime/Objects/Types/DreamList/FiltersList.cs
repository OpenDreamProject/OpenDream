using System.Linq;
using OpenDreamShared.Dream;
namespace OpenDreamRuntime.Objects.Types;


// atom.filters list
// Operates on an object's appearance
public sealed class DreamFilterList(DreamObjectDefinition listDef, DreamObject owner) : DreamListAbstract(listDef) {
    public override void Cut(int start = 1, int end = 0) {
        AtomManager.UpdateAppearance(owner, appearance => {
            int filterCount = appearance.Filters.Count + 1;
            if (end == 0 || end > filterCount) end = filterCount;

            appearance.Filters.RemoveRange(start - 1, end - start);
        });
    }

    public int GetIndexOfFilter(DreamFilter filter) {
        ImmutableAppearance appearance = GetAppearance();
        for (int i = 0; i < appearance.Filters.Length; i++) {
            if (appearance.Filters[i] == filter)
                return i;
        }

        return -1;
    }

    public int GetIndexOfFilter(string filterName) {
        ImmutableAppearance appearance = GetAppearance();
        for (int i = 0; i < appearance.Filters.Length; i++) {
            if (appearance.Filters[i].FilterName == filterName)
                return i;
        }

        return -1;
    }

    public void SetFilter(int index, DreamFilter? filter) {
        AtomManager.UpdateAppearance(owner, appearance => {
            if (index < 1 || index > appearance.Filters.Count)
                throw new DMException($"Cannot index {index} on filter list");

            DreamFilter oldFilter = appearance.Filters[index - 1];

            DreamObjectFilter.FilterAttachedTo.Remove(oldFilter);

            if (filter == null) { // Setting an index to null is the same as removing it ("filters[1] = null")
                appearance.Filters.RemoveAt(index - 1);
            } else {
                appearance.Filters[index - 1] = filter;
                DreamObjectFilter.FilterAttachedTo[filter] = this;
            }
        });
    }

    public override DreamValue GetElement(DreamValue key) {
        int filterIndex;
        if (key.TryGetValueAsString(out var filterName)) {
            filterIndex = GetIndexOfFilter(filterName) + 1; // We're 1-indexed while GetIndexOfFilter() is not
            if (filterIndex < 1) // If a filter by the name doesn't exist, it returns null
                return DreamValue.Null;
        } else {
            key.TryGetValueAsInteger(out filterIndex);
        }

        ImmutableAppearance appearance = GetAppearance();
        if (filterIndex < 1 || filterIndex > appearance.Filters.Length)
            throw new DMException($"Atom only has {appearance.Filters.Length} filter(s), cannot index {filterIndex}");

        DreamObjectFilter filterObject = ObjectTree.CreateObject<DreamObjectFilter>(ObjectTree.Filter);
        filterObject.Filter = appearance.Filters[filterIndex - 1];
        return new DreamValue(filterObject);
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        ImmutableAppearance appearance = GetAppearance();

        foreach (var filter in appearance.Filters) {
            DreamObjectFilter filterObject = ObjectTree.CreateObject<DreamObjectFilter>(ObjectTree.Filter);
            filterObject.Filter = filter;

            yield return new DreamValue(filterObject);
            filterObject.DecRef();
        }
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject) && !value.IsNull)
            throw new DMException($"Cannot set value of filter list to {value}");
        if (!key.TryGetValueAsInteger(out var filterIndex) || filterIndex < 1)
            throw new DMException($"Invalid index into filter list: {key}");

        SetFilter(filterIndex, filterObject?.Filter);
    }

    public override void AddKey(DreamValue value) {
        if (value.IsNull) // "filters += null" is just ignored
            return;
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject))
            throw new DMException($"Cannot add {value} to filter list");

        //This is dynamic to prevent the compiler from optimising the SerializationManager.CreateCopy() call to the DreamFilter type
        //so we can preserve the subclass information. Setting it to DreamFilter instead will cause filter parameters to stop working.
        dynamic filter = filterObject.Filter;
        DreamFilter copy = SerializationManager.CreateCopy(filter, notNullableOverride: true); // Adding a filter creates a copy

        DreamObjectFilter.FilterAttachedTo[copy] = this;
        AtomManager.UpdateAppearance(owner, appearance => {
            appearance.Filters.Add(copy);
        });
    }

    public override void RemoveKey(DreamValue value) {
        int filterIndex = -1;
        if (value.TryGetValueAsString(out var filterName)) { // You can also do atom.filters -= "name"
            filterIndex = GetIndexOfFilter(filterName);
        } else if (value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject)) {
            filterIndex = GetIndexOfFilter(filterObject.Filter);
        }

        if (filterIndex < 0) // Failed to find the filter in the list
            return;

        AtomManager.UpdateAppearance(owner, appearance => {
            DreamObjectFilter.FilterAttachedTo.Remove(appearance.Filters[filterIndex]);
            appearance.Filters.RemoveAt(filterIndex);
        });
    }

    public override bool ContainsKey(DreamValue element) {
        return EnumerateKeys().Contains(element);
    }

    public override int GetLength() {
        return GetAppearance().Filters.Length;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    private ImmutableAppearance GetAppearance() {
        ImmutableAppearance? appearance = AtomManager.MustGetAppearance(owner);
        if (appearance == null)
            throw new DMException("Atom has no appearance");

        return appearance;
    }
}
