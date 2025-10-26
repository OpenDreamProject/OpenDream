using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;

namespace OpenDreamRuntime.Procs;

public interface IDreamValueEnumerator {
    /// <summary>
    /// Perform the next enumeration, used to advance the state of many types of loops.
    /// </summary>
    /// <param name="state">The proc state that is executing this</param>
    /// <param name="reference">The var to assign the output to</param>
    /// <param name="assocReference">The var to assign an associated value to, for use by key-value pair loops</param>
    /// <returns>Whether the enumeration succeeded or not</returns>
    public bool Enumerate(DMProcState state, DreamReference reference, DreamReference assocReference);
}

/// <summary>
/// Enumerates a range of numbers with a given step
/// <code>for (var/i in 1 to 10 step 2)</code>
/// </summary>
internal sealed class DreamValueRangeEnumerator(float rangeStart, float rangeEnd, float step) : IDreamValueEnumerator {
    private float _current = rangeStart - step;

    public bool Enumerate(DMProcState state, DreamReference reference, DreamReference assocReference) {
        _current += step;

        bool successful = (step > 0) ? _current <= rangeEnd : _current >= rangeEnd;
        if (successful) { // Only assign if it was successful
            state.AssignReference(reference, new DreamValue(_current));
            state.AssignReference(assocReference, DreamValue.Null);
        }

        return successful;
    }
}

/// <summary>
/// Enumerates over an IEnumerable of DreamObjects, possibly filtering for a certain type
/// </summary>
internal sealed class DreamObjectEnumerator(IEnumerable<DreamObject> dreamObjects, TreeEntry? filterType = null) : IDreamValueEnumerator {
    private readonly IEnumerator<DreamObject> _dreamObjectEnumerator = dreamObjects.GetEnumerator();

    public bool Enumerate(DMProcState state, DreamReference reference, DreamReference assocReference) {
        bool success = _dreamObjectEnumerator.MoveNext();

        while(success && _dreamObjectEnumerator.Current.Deleted) //skip over deleted
            success = _dreamObjectEnumerator.MoveNext();

        if (filterType != null) {
            while (success && (_dreamObjectEnumerator.Current.Deleted || !_dreamObjectEnumerator.Current.IsSubtypeOf(filterType))) {
                success = _dreamObjectEnumerator.MoveNext();
            }
        }

        // Assign regardless of success
        state.AssignReference(reference, success ? new DreamValue(_dreamObjectEnumerator.Current) : DreamValue.Null);
        state.AssignReference(assocReference, DreamValue.Null);
        return success;
    }
}

/// <summary>
/// Enumerates over an array of DreamValues
/// <code>for (var/i in list(1, 2, 3))</code>
/// </summary>
internal sealed class DreamValueArrayEnumerator(DreamValue[] values, Dictionary<DreamValue, DreamValue>? assocValues) : IDreamValueEnumerator {
    private int _current = -1;

    public bool Enumerate(DMProcState state, DreamReference reference, DreamReference assocReference) {
        _current++;

        bool success = _current < values.Length;
        var value = success ? values[_current] : DreamValue.Null;

        // Assign regardless of success
        state.AssignReference(reference, value);
        if (assocReference != DreamReference.NoRef)
            state.AssignReference(assocReference, assocValues?.GetValueOrDefault(value, DreamValue.Null) ?? DreamValue.Null);
        return success;
    }
}

/// <summary>
/// Enumerates over an array of DreamValues, filtering for a certain type
/// <code>for (var/obj/item/I in contents)</code>
/// </summary>
internal sealed class FilteredDreamValueArrayEnumerator(DreamValue[] values, Dictionary<DreamValue, DreamValue>? assocValues, TreeEntry filterType)
    : IDreamValueEnumerator {
    private int _current = -1;

    public bool Enumerate(DMProcState state, DreamReference reference, DreamReference assocReference) {
        do {
            _current++;
            if (_current >= values.Length) {
                state.AssignReference(reference, DreamValue.Null);
                state.AssignReference(assocReference, DreamValue.Null);
                return false;
            }

            DreamValue value = values[_current];
            if (value.TryGetValueAsDreamObject(out var dreamObject) && (dreamObject?.IsSubtypeOf(filterType) ?? false)) {
                state.AssignReference(reference, value);
                if (assocReference != DreamReference.NoRef)
                    state.AssignReference(assocReference, assocValues?.GetValueOrDefault(value, DreamValue.Null) ?? DreamValue.Null);
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

    public bool Enumerate(DMProcState state, DreamReference reference, DreamReference assocReference) {
        bool success = _enumerator.MoveNext();

        // Assign regardless of success
        state.AssignReference(reference, new DreamValue(_enumerator.Current));
        state.AssignReference(assocReference, DreamValue.Null);
        return success;
    }
}
