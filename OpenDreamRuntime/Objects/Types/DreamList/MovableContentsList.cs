using System.Linq;
namespace OpenDreamRuntime.Objects.Types;

// mob.contents, obj.contents list
public sealed class MovableContentsList(DreamObjectDefinition listDef, DreamObjectMovable owner, TransformComponent transform) : DreamListAbstract(listDef) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into movable contents list: {key}");
        if (index < 1 || index > transform.ChildCount)
            throw new DMException($"Out of bounds index on movable contents list: {index}");

        using var childEnumerator = transform.ChildEnumerator;
        while (index >= 1) {
            childEnumerator.MoveNext(out EntityUid child);

            if (index == 1) {
                if (AtomManager.TryGetMovableFromEntity(child, out var childObject)) {
                    childObject.IncRef();
                    return new DreamValue(childObject);
                } else
                    throw new DMException($"Invalid child in movable contents list: {child}");
            }

            index--;
        }

        throw new DMException($"Out of bounds index on movable contents list after iterating: {key}");
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        using var childEnumerator = transform.ChildEnumerator;

        while (childEnumerator.MoveNext(out EntityUid child)) {
            if (!AtomManager.TryGetMovableFromEntity(child, out var childObject))
                continue;

            yield return new DreamValue(childObject);
        }
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        if (end == 0 || end > transform.ChildCount) end = transform.ChildCount;

        using var childEnumerator = transform.ChildEnumerator;

        int i = 0;
        while (childEnumerator.MoveNext(out EntityUid child)) {
            if (!AtomManager.TryGetMovableFromEntity(child, out var childObject))
                continue;
            i++;
            if (i >= start && new DreamValue(childObject).Equals(value))
                return i;
            if (i > end)
                return 0;
        }

        return 0;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set an index of movable contents list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var dreamObject))
            throw new DMException($"Cannot add {value} to movable contents");

        dreamObject.SetLoc(owner);
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            throw new DMException($"Cannot remove {value} from movable contents");
        if (movable.Loc != owner)
            return; // This object wasn't in our contents to begin with

        movable.SetLoc(null);
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var dreamObject))
            return false;

        return dreamObject.Loc == owner;
    }

    public override void Cut(int start = 1, int end = 0) {
        // TODO
    }

    public override int GetLength() {
        return transform.ChildCount;
    }
}
