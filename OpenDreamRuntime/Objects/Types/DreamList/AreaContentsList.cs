using System.Linq;
namespace OpenDreamRuntime.Objects.Types;

// area.contents list
public sealed class AreaContentsList(DreamObjectDefinition listDef, DreamObjectArea area) : DreamList(listDef) {
    public override bool IsAssociative => false;

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into area contents list: {key}");

        foreach (var turf in area.Turfs) {
            if (index < 1)
                break;

            if (index == 1) { // The index references this turf
                turf.IncRef();
                return new(turf);
            }

            index -= 1;
            int contentsLength = turf.Contents.GetLength();

            if (index <= contentsLength) { // The index references one of the turf's contents
                var contentsItem = turf.Contents.GetElement(new(index));
                return contentsItem;
            }

            index -= contentsLength;
        }

        throw new DMException($"Out of bounds index on turf contents list: {key}");
    }

    public override List<DreamValue> CopyKeys() {
        return Enumerate().ToList();
    }

    public override IEnumerable<DreamValue> Enumerate() {
        foreach (var turf in area.Turfs) {
            yield return new(turf);

            foreach (var content in turf.Contents.Enumerate())
                yield return content;
        }
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set an index of area contents list");
    }

    public override void Add(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf))
            throw new DMException($"Cannot add {value} to area contents");

        turf.Cell.Area = area;
    }

    public override void Remove(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf))
            throw new DMException($"Cannot remove {value} from area contents");

        turf.Cell.Area = DreamMapManager.DefaultArea;
    }

    public override int GetLength() {
        int length = area.Turfs.Count;

        foreach (var turf in area.Turfs)
            length += turf.Contents.GetLength();

        return length;
    }

    public override int FindIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectAtom>(out var atom))
            return false;

        if (atom is DreamObjectArea) // areas do not contain themselves
            return false;

        var (x, y, z) = AtomManager.GetAtomPosition(atom);
        if (!DreamMapManager.TryGetCellAt((x, y), z, out var cell))
            return false;

        return cell.Area == area;
    }

    public override void Swap(int index1, int index2) {
        throw new NotImplementedException();
    }

    public override void Cut(int start = 1, int end = 0) {
        // TODO
    }

    public override void Insert(int index, DreamValue value) {
        throw new NotImplementedException();
    }
}
