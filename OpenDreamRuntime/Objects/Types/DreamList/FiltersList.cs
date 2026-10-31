using System.Linq;
using OpenDreamShared.Dream;
namespace OpenDreamRuntime.Objects.Types;


// atom.filters list
// Operates on an object's appearance
public sealed class FiltersList(DreamObjectDefinition listDef, DreamObject owner) : DreamList(listDef) {
    /// <summary>
    /// One object per filter on this list, reused every time that filter is read so all references see the same values
    /// </summary>
    private readonly List<DreamObjectFilter> _filterObjects = [];

    protected override void HandleDeletion() {
        foreach (var filterObject in _filterObjects)
            filterObject.DecRef();

        _filterObjects.Clear();
        base.HandleDeletion();
    }

    public override void Cut(int start = 1, int end = 0) {
        AtomManager.UpdateAppearance(owner, appearance => {
            int filterCount = appearance.Filters.Count + 1;
            if (end == 0 || end > filterCount) end = filterCount;

            appearance.Filters.RemoveRange(start - 1, end - start);
        });
    }

    public int GetIndexOfFilter(DreamFilter filter) => Array.IndexOf(GetAppearance().Filters, filter);

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

            if (filter == null) { // Setting an index to null is the same as removing it ("filters[1] = null")
                appearance.Filters.RemoveAt(index - 1);
            } else {
                appearance.Filters[index - 1] = filter;
            }
        });
    }

    /// <summary>
    /// Replaces every filter equal to <paramref name="oldFilter"/>, when a filter var is written.
    /// </summary>
    /// <exception cref="DMException">The filter is no longer on this list, or its owner was deleted</exception>
    public void ReplaceFilter(DreamFilter oldFilter, DreamFilter newFilter, string varName) {
        if (owner.Deleted)
            throw new DMException($"Cannot set var \"{varName}\" on filter of deleted atom");
        if (GetIndexOfFilter(oldFilter) == -1)
            throw new DMException($"Cannot set var \"{varName}\" on filter removed from {owner}");

        AtomManager.UpdateAppearance(owner, appearance => {
            var filters = appearance.Filters;
            for (int i = 0; i < filters.Count; i++) {
                if (filters[i] == oldFilter)
                    filters[i] = newFilter;
            }
        });

        foreach (var filterObject in _filterObjects) {
            if (filterObject.Filter == oldFilter)
                filterObject.Filter = newFilter;
        }
    }

    /// <summary>
    /// Gets the list's filter object for a filter, creating it if needed.
    /// Also releases the objects of filters that were removed from the appearance.
    /// Writing to one of those afterwards is a runtime error.
    /// </summary>
    private DreamObjectFilter GetFilterObject(DreamFilter[] filters, DreamFilter filter) {
        _filterObjects.RemoveAll(filterObject => {
            if (Array.IndexOf(filters, filterObject.Filter) != -1)
                return false;

            filterObject.DecRef();
            return true;
        });

        foreach (var filterObject in _filterObjects) {
            if (filterObject.Filter == filter)
                return filterObject;
        }

        var newObject = ObjectTree.CreateObject<DreamObjectFilter>(ObjectTree.Filter);
        newObject.Filter = filter;
        newObject.AttachedTo = this;
        _filterObjects.Add(newObject);
        return newObject;
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

        DreamObjectFilter filterObject = GetFilterObject(appearance.Filters, appearance.Filters[filterIndex - 1]);
        filterObject.IncRef();
        return new DreamValue(filterObject);
    }

    public override List<DreamValue> CopyKeys() {
        return Enumerate().ToList();
    }

    public override IEnumerable<DreamValue> Enumerate() {
        ImmutableAppearance appearance = GetAppearance();

        foreach (var filter in appearance.Filters) {
            yield return new DreamValue(GetFilterObject(appearance.Filters, filter));
        }
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject) && !value.IsNull)
            throw new DMException($"Cannot set value of filter list to {value}");
        if (!key.TryGetValueAsInteger(out var filterIndex) || filterIndex < 1)
            throw new DMException($"Invalid index into filter list: {key}");

        SetFilter(filterIndex, filterObject?.Filter);
    }

    public override void Add(DreamValue value) {
        if (value.IsNull) // "filters += null" is just ignored
            return;
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject))
            throw new DMException($"Cannot add {value} to filter list");

        // Sharing the filter is safe because writing a var replaces it instead of changing it
        AtomManager.UpdateAppearance(owner, appearance => {
            appearance.Filters.Add(filterObject.Filter);
        });
    }

    public override void Remove(DreamValue value) {
        int filterIndex = -1;
        if (value.TryGetValueAsString(out var filterName)) { // You can also do atom.filters -= "name"
            filterIndex = GetIndexOfFilter(filterName);
        } else if (value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject)) {
            filterIndex = GetIndexOfFilter(filterObject.Filter);
        }

        if (filterIndex < 0) // Failed to find the filter in the list
            return;

        AtomManager.UpdateAppearance(owner, appearance => {
            appearance.Filters.RemoveAt(filterIndex);
        });
    }

    public override bool ContainsKey(DreamValue element) {
        return Enumerate().Contains(element);
    }

    public override int GetLength() {
        return GetAppearance().Filters.Length;
    }

    public override int FindIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    private ImmutableAppearance GetAppearance() {
        ImmutableAppearance? appearance = AtomManager.MustGetAppearance(owner);
        if (appearance == null)
            throw new DMException("Atom has no appearance");

        return appearance;
    }
}
