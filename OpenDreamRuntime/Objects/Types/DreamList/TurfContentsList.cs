using System.Linq;
using OpenDreamRuntime.Map;
namespace OpenDreamRuntime.Objects.Types;

// turf.contents list
public sealed class TurfContentsList(DreamObjectDefinition listDef, DreamObjectTurf turf) : DreamList(listDef) {
    private IDreamMapManager.Cell Cell => turf.Cell;

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into turf contents list: {key}");
        if (index < 1 || index > Cell.Movables.Count)
            throw new DMException($"Out of bounds index on turf contents list: {index}");

        var value = Cell.Movables[index - 1];
        value.IncRef();
        return new DreamValue(value);
    }

    public override List<DreamValue> CopyKeys() {
        return Enumerate().ToList();
    }

    public override IEnumerable<DreamValue> Enumerate() {
        foreach (var movable in Cell.Movables)
            yield return new(movable);
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set an index of turf contents list");
    }

    public override void Add(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            throw new DMException($"Cannot add {value} to turf contents");

        movable.SetLoc(Cell.Turf);
    }

    public override void Remove(DreamValue element) {
        throw new NotImplementedException();
    }

    public override void Cut(int start = 1, int end = 0) {
        int movableCount = Cell.Movables.Count + 1;
        if (end == 0 || end > movableCount) end = movableCount;

        for (int i = start; i < end; i++) {
            Cell.Movables[i - 1].SetLoc(null);
        }
    }

    public override int GetLength() {
        return Cell.Movables.Count;
    }

    public override int FindIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var dreamObject))
            return false;

        return dreamObject.Loc == turf;
    }
}
