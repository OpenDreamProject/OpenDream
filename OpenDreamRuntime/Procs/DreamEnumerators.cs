using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;

namespace OpenDreamRuntime.Procs;

public interface IDreamValueEnumerator {
    public bool Enumerate(DMProcState state, DreamReference? reference, bool peekAssignment);
}

/// <summary>
/// Enumerates a range of numbers with a given step
/// <code>for (var/i in 1 to 10 step 2)</code>
/// </summary>
internal sealed class DreamValueRangeEnumerator(float rangeStart, float rangeEnd, float step) : IDreamValueEnumerator {
    private float _current = rangeStart - step;

    public bool Enumerate(DMProcState state, DreamReference? reference, bool peekAssignment) {
        _current += step;

        bool successful = (step > 0) ? _current <= rangeEnd : _current >= rangeEnd;
        if (successful && reference != null) // Only assign if it was successful
            state.AssignReference(reference.Value, new DreamValue(_current), peekAssignment);

        return successful;
    }
}

/// <summary>
/// Enumerates over an IEnumerable of DreamObjects, possibly filtering for a certain type
/// </summary>
internal sealed class DreamObjectEnumerator(IEnumerable<DreamObject> dreamObjects, TreeEntry? filterType = null) : IDreamValueEnumerator {
    private readonly IEnumerator<DreamObject> _dreamObjectEnumerator = dreamObjects.GetEnumerator();

    public bool Enumerate(DMProcState state, DreamReference? reference, bool peekAssignment) {
        bool success = _dreamObjectEnumerator.MoveNext();

        while(success && _dreamObjectEnumerator.Current.Deleted) //skip over deleted
            success = _dreamObjectEnumerator.MoveNext();

        if (filterType != null) {
            while (success && (_dreamObjectEnumerator.Current.Deleted || !_dreamObjectEnumerator.Current.IsSubtypeOf(filterType))) {
                success = _dreamObjectEnumerator.MoveNext();
            }
        }

        // Assign regardless of success
        if (reference != null)
            state.AssignReference(reference.Value,
                success ? new DreamValue(_dreamObjectEnumerator.Current) : DreamValue.Null, peekAssignment);
        return success;
    }
}

/// <summary>
/// Enumerates over an array of DreamValues
/// <code>for (var/i in list(1, 2, 3))</code>
/// </summary>
internal sealed class DreamValueArrayEnumerator(DreamValue[] dreamValueArray) : IDreamValueEnumerator {
    private int _current = -1;

    public bool Enumerate(DMProcState state, DreamReference? reference, bool peekAssignment) {
        _current++;

        bool success = _current < dreamValueArray.Length;
        if (reference != null) // Assign regardless of success
            state.AssignReference(reference.Value, success ? dreamValueArray[_current] : DreamValue.Null, peekAssignment);
        return success;
    }
}

/// <summary>
/// Enumerates over an array of DreamValues, filtering for a certain type
/// <code>for (var/obj/item/I in contents)</code>
/// </summary>
internal sealed class FilteredDreamValueArrayEnumerator(DreamValue[] dreamValueArray, TreeEntry filterType) : IDreamValueEnumerator {
    private int _current = -1;

    public bool Enumerate(DMProcState state, DreamReference? reference, bool peekAssignment) {
        do {
            _current++;
            if (_current >= dreamValueArray.Length) {
                if (reference != null)
                    state.AssignReference(reference.Value, DreamValue.Null, peekAssignment);
                return false;
            }

            DreamValue value = dreamValueArray[_current];
            if (value.TryGetValueAsDreamObject(out var dreamObject) && (dreamObject?.IsSubtypeOf(filterType) ?? false)) {
                if (reference != null)
                    state.AssignReference(reference.Value, value, peekAssignment);
                return true;
            }
        } while (true);
    }
}

/// <summary>
/// Enumerates over all atoms in the world, possibly filtering for a certain type
/// <code>for (var/obj/item/I in world)</code>
/// </summary>
internal sealed class WorldContentsEnumerator(AtomManager atomManager, TreeEntry? filterType) : IDreamValueEnumerator {
    private readonly IEnumerator<DreamObjectAtom> _enumerator = atomManager.EnumerateAtoms(filterType).GetEnumerator();

    public bool Enumerate(DMProcState state, DreamReference? reference, bool peekAssignment) {
        bool success = _enumerator.MoveNext();

        // Assign regardless of success
        if (reference != null)
            state.AssignReference(reference.Value, new DreamValue(_enumerator.Current), peekAssignment);
        return success;
    }
}
