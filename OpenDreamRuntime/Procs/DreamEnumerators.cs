using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    public interface IDreamValueEnumerator {
        public bool Enumerate(DMProcState state, DreamReference? reference);
        public void EndEnumeration();
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

        public bool Enumerate(DMProcState state, DreamReference? reference) {
            _current += _step;

            bool successful = (_step > 0) ? _current <= _end : _current >= _end;
            if (successful && reference != null) // Only assign if it was successful
                state.AssignReference(reference.Value, new DreamValue(_current));

            return successful;
        }

        void IDreamValueEnumerator.EndEnumeration() {
        }
    }

    /// <summary>
    /// Enumerates over an IEnumerable of DreamObjects, possibly filtering for a certain type
    /// </summary>
    sealed class DreamObjectEnumerator : IDreamValueEnumerator {
        private readonly IEnumerator<DreamObject> _dreamObjectEnumerator;
        private readonly TreeEntry? _filterType;

        public DreamObjectEnumerator(IEnumerable<DreamObject> dreamObjects, TreeEntry? filterType = null) {
            _dreamObjectEnumerator = dreamObjects.GetEnumerator();
            _filterType = filterType;
        }

        public bool Enumerate(DMProcState state, DreamReference? reference) {
            bool success = _dreamObjectEnumerator.MoveNext();
            if (_filterType != null) {
                while (success && !_dreamObjectEnumerator.Current.IsSubtypeOf(_filterType)) {
                    success = _dreamObjectEnumerator.MoveNext();
                }
            }

            // Assign regardless of success
            if (reference != null)
                state.AssignReference(reference.Value, new DreamValue(_dreamObjectEnumerator.Current));
            return success;
        }

        void IDreamValueEnumerator.EndEnumeration() {
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

        public bool Enumerate(DMProcState state, DreamReference? reference) {
            _current++;

            bool success = _current < _dreamValueArray.Length;
            if (reference != null)
                state.AssignReference(reference.Value, success ? _dreamValueArray[_current] : DreamValue.Null); // Assign regardless of success
            return success;
        }

        void IDreamValueEnumerator.EndEnumeration() {
        }
    }

    /// <summary>
    /// Enumerates over an array of DreamValues, filtering for a certain type
    /// <code>for (var/obj/item/I in contents)</code>
    /// </summary>
    sealed class FilteredDreamValueArrayEnumerator : IDreamValueEnumerator {
        private readonly DreamValue[] _dreamValueArray;
        private readonly TreeEntry _filterType;
        private int _current = -1;

        public FilteredDreamValueArrayEnumerator(DreamValue[] dreamValueArray, TreeEntry filterType) {
            _dreamValueArray = dreamValueArray;
            _filterType = filterType;
        }

        public bool Enumerate(DMProcState state, DreamReference? reference) {
            do {
                _current++;
                if (_current >= _dreamValueArray.Length) {
                    if (reference != null)
                        state.AssignReference(reference.Value, DreamValue.Null);
                    return false;
                }

                DreamValue value = _dreamValueArray[_current];
                if (value.TryGetValueAsDreamObject(out var dreamObject) && (dreamObject?.IsSubtypeOf(_filterType) ?? false)) {
                    if (reference != null)
                        state.AssignReference(reference.Value, value);
                    return true;
                }
            } while (true);
        }

        void IDreamValueEnumerator.EndEnumeration() {
        }
    }

    /// <summary>
    /// Enumerates over all atoms in the world, possibly filtering for a certain type
    /// <code>for (var/obj/item/I in world)</code>
    /// </summary>
    sealed class WorldContentsEnumerator : IDreamValueEnumerator {
        private readonly AtomManager _atomManager;
        private readonly TreeEntry? _filterType;
        private int _current = -1;
        private int _startCount = 0;

        public WorldContentsEnumerator(AtomManager atomManager, TreeEntry? filterType) {
            _atomManager = atomManager;
            _filterType = filterType;
            if(filterType is not null)
                if(filterType.ObjectDefinition.IsSubtypeOf(filterType.ObjectDefinition.ObjectTree.Area)) {
                    atomManager.Areas.StartEnumeration();
                    _startCount = atomManager.Areas.Count;
                } else if(filterType.ObjectDefinition.IsSubtypeOf(filterType.ObjectDefinition.ObjectTree.Mob)) {
                    atomManager.Mobs.StartEnumeration();
                    _startCount = atomManager.Mobs.Count;
                } else if(filterType.ObjectDefinition.IsSubtypeOf(filterType.ObjectDefinition.ObjectTree.Obj)) {
                    atomManager.Objects.StartEnumeration();
                    _startCount = atomManager.Objects.Count;
                } else if(filterType.ObjectDefinition.IsSubtypeOf(filterType.ObjectDefinition.ObjectTree.Turf)) {
                    atomManager.Turfs.StartEnumeration();
                    _startCount = atomManager.Turfs.Count;
                } else if(filterType.ObjectDefinition.IsSubtypeOf(filterType.ObjectDefinition.ObjectTree.Movable)) {
                    atomManager.Movables.StartEnumeration();
                    _startCount = atomManager.Movables.Count;
                }
            else {
                atomManager.Areas.StartEnumeration();
                atomManager.Mobs.StartEnumeration();
                atomManager.Objects.StartEnumeration();
                atomManager.Turfs.StartEnumeration();
                atomManager.Movables.StartEnumeration();
                _startCount = atomManager.AtomCount;
            }
        }

        public bool Enumerate(DMProcState state, DreamReference? reference) {
            do {
                if(_filterType is null){
                    if(_startCount != _atomManager.AtomCount){
                        _current = _current - Math.Max(0, _startCount - _atomManager.AtomCount); //if we got smaller, we need to adjust our current index
                        _startCount = _atomManager.AtomCount;
                    }
                } else {
                    if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Area)) {
                        if(_startCount != _atomManager.Areas.Count){
                            _current = _current - Math.Max(0, _startCount - _atomManager.Areas.Count); //if we got smaller, we need to adjust our current index
                            _startCount = _atomManager.Areas.Count;
                        }
                    } else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Mob)) {
                        if(_startCount != _atomManager.Mobs.Count){
                            _current = _current - Math.Max(0, _startCount - _atomManager.Mobs.Count); //if we got smaller, we need to adjust our current index
                            _startCount = _atomManager.Mobs.Count;
                        }
                    } else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Obj)) {
                        if(_startCount != _atomManager.Objects.Count){
                            _current = _current - Math.Max(0, _startCount - _atomManager.Objects.Count); //if we got smaller, we need to adjust our current index
                            _startCount = _atomManager.Objects.Count;
                        }
                    } else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Turf)) {
                        if(_startCount != _atomManager.Turfs.Count){
                            _current = _current - Math.Max(0, _startCount - _atomManager.Turfs.Count); //if we got smaller, we need to adjust our current index
                            _startCount = _atomManager.Turfs.Count;
                        }
                    } else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Movable)) {
                        if(_startCount != _atomManager.Movables.Count){
                            _current = _current - Math.Max(0, _startCount - _atomManager.Movables.Count); //if we got smaller, we need to adjust our current index
                            _startCount = _atomManager.Movables.Count;
                        }
                    }
                }
                _current++;
                if (_current >= _atomManager.AtomCount) {
                    if (reference != null)
                        state.AssignReference(reference.Value, DreamValue.Null);
                    return false;
                }

                DreamObject atom = _atomManager.GetAtom(_current);
                if (!atom.Deleted && (_filterType == null || atom.IsSubtypeOf(_filterType))) {
                    if (reference != null)
                        state.AssignReference(reference.Value, new DreamValue(atom));
                    return true;
                }
            } while (true);
        }

        void IDreamValueEnumerator.EndEnumeration() {
            if(_filterType is not null)
                if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Area))
                    _atomManager.Areas.FinishEnumeration();
                else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Mob))
                    _atomManager.Mobs.FinishEnumeration();
                else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Obj))
                    _atomManager.Objects.FinishEnumeration();
                else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Turf))
                    _atomManager.Turfs.FinishEnumeration();
                else if(_filterType.ObjectDefinition.IsSubtypeOf(_filterType.ObjectDefinition.ObjectTree.Movable))
                    _atomManager.Movables.FinishEnumeration();
            else {
                _atomManager.Areas.FinishEnumeration();
                _atomManager.Mobs.FinishEnumeration();
                _atomManager.Objects.FinishEnumeration();
                _atomManager.Turfs.FinishEnumeration();
                _atomManager.Movables.FinishEnumeration();
            }
        }
    }
}
