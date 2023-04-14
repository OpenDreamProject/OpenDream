﻿using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    internal interface IDreamValueEnumerator {
        public bool Enumerate(DMProcState state, DMReference reference);
    }

    /// <summary>
    /// Enumerates a range of numbers with a given step
    /// <code>for (var/i in 1 to 10 step 2)</code>
    /// </summary>
    sealed class DreamValueRangeEnumerator : IDreamValueEnumerator {
        private float _current;
        private readonly float _end;
        private readonly float _step;

        public DreamValueRangeEnumerator(float rangeStart, float rangeEnd, float step) {
            _current = rangeStart - step;
            _end = rangeEnd;
            _step = step;
        }

        public bool Enumerate(DMProcState state, DMReference reference) {
            _current += _step;

            bool successful = (_step > 0) ? _current <= _end : _current >= _end;
            if (successful) // Only assign if it was successful
                state.AssignReference(reference, new DreamValue(_current));

            return successful;
        }
    }

    /// <summary>
    /// Enumerates over an IEnumerable of DreamObjects, possibly filtering for a certain type
    /// </summary>
    sealed class DreamObjectEnumerator : IDreamValueEnumerator {
        private readonly IEnumerator<DreamObject> _dreamObjectEnumerator;
        private readonly IDreamObjectTree.TreeEntry? _filterType;

        public DreamObjectEnumerator(IEnumerable<DreamObject> dreamObjects, IDreamObjectTree.TreeEntry? filterType = null) {
            _dreamObjectEnumerator = dreamObjects.GetEnumerator();
            _filterType = filterType;
        }

        public bool Enumerate(DMProcState state, DMReference reference) {
            bool success = _dreamObjectEnumerator.MoveNext();
            if (_filterType != null) {
                while (success && !_dreamObjectEnumerator.Current.IsSubtypeOf(_filterType)) {
                    success = _dreamObjectEnumerator.MoveNext();
                }
            }

            // Assign regardless of success
            state.AssignReference(reference, new DreamValue(_dreamObjectEnumerator.Current));
            return success;
        }
    }

    /// <summary>
    /// Enumerates over an array of DreamValues
    /// <code>for (var/i in list(1, 2, 3))</code>
    /// </summary>
    sealed class DreamValueArrayEnumerator : IDreamValueEnumerator {
        private readonly DreamValue[] _dreamValueArray;
        private int _current = -1;

        public DreamValueArrayEnumerator(DreamValue[] dreamValueArray) {
            _dreamValueArray = dreamValueArray;
        }

        public bool Enumerate(DMProcState state, DMReference reference) {
            _current++;

            bool success = _current < _dreamValueArray.Length;
            state.AssignReference(reference, success ? _dreamValueArray[_current] : DreamValue.Null); // Assign regardless of success
            return success;
        }
    }

    /// <summary>
    /// Enumerates over an array of DreamValues, filtering for a certain type
    /// <code>for (var/obj/item/I in contents)</code>
    /// </summary>
    sealed class FilteredDreamValueArrayEnumerator : IDreamValueEnumerator {
        private readonly DreamValue[] _dreamValueArray;
        private readonly IDreamObjectTree.TreeEntry _filterType;
        private int _current = -1;

        public FilteredDreamValueArrayEnumerator(DreamValue[] dreamValueArray, IDreamObjectTree.TreeEntry filterType) {
            _dreamValueArray = dreamValueArray;
            _filterType = filterType;
        }

        public bool Enumerate(DMProcState state, DMReference reference) {
            do {
                _current++;
                if (_current >= _dreamValueArray.Length) {
                    state.AssignReference(reference, DreamValue.Null);
                    return false;
                }

                DreamValue value = _dreamValueArray[_current];
                if (value.TryGetValueAsDreamObjectOfType(_filterType, out _)) {
                    state.AssignReference(reference, value);
                    return true;
                }
            } while (true);
        }
    }

    /// <summary>
    /// Enumerates over all atoms in the world, possibly filtering for a certain type
    /// <code>for (var/obj/item/I in world)</code>
    /// </summary>
    sealed class WorldContentsEnumerator : IDreamValueEnumerator {
        private readonly IDreamMapManager _mapManager;
        private readonly IDreamObjectTree.TreeEntry? _filterType;
        private int _current = -1;

        public WorldContentsEnumerator(IDreamMapManager mapManager, IDreamObjectTree.TreeEntry? filterType) {
            _mapManager = mapManager;
            _filterType = filterType;
        }

        public bool Enumerate(DMProcState state, DMReference reference) {
            do {
                _current++;
                if (_current >= _mapManager.AllAtoms.Count) {
                    state.AssignReference(reference, DreamValue.Null);
                    return false;
                }

                DreamObject atom = _mapManager.AllAtoms[_current];
                if (_filterType == null || atom.IsSubtypeOf(_filterType)) {
                    state.AssignReference(reference, new DreamValue(atom));
                    return true;
                }
            } while (true);
        }
    }
}
