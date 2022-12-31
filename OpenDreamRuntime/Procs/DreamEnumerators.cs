using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs {
    public interface IDreamValueEnumerator {
        public DreamValue Current { get; }

        public bool MoveNext();
    }

    /// <summary>
    /// Enumerates a range of numbers with a given step
    /// <code>for (var/i in 1 to 10 step 2)</code>
    /// </summary>
    sealed class DreamValueRangeEnumerator : IDreamValueEnumerator {
        public DreamValue Current => new DreamValue(_current);

        private float _current;
        private readonly float _end;
        private readonly float _step;

        public DreamValueRangeEnumerator(float rangeStart, float rangeEnd, float step) {
            _current = rangeStart - step;
            _end = rangeEnd;
            _step = step;
        }

        public bool MoveNext() {
            _current += _step;

            return (_step > 0) ? _current <= _end : _current >= _end;
        }
    }

    /// <summary>
    /// Enumerates over an IEnumerable of DreamObjects, possibly filtering for a certain type
    /// </summary>
    sealed class DreamObjectEnumerator : IDreamValueEnumerator {
        public DreamValue Current => new DreamValue(_dreamObjectEnumerator.Current);

        private readonly IEnumerator<DreamObject> _dreamObjectEnumerator;
        private readonly IDreamObjectTree.TreeEntry? _filterType;

        public DreamObjectEnumerator(IEnumerable<DreamObject> dreamObjects, IDreamObjectTree.TreeEntry? filterType = null) {
            _dreamObjectEnumerator = dreamObjects.GetEnumerator();
            _filterType = filterType;
        }

        public bool MoveNext() {
            bool hasNext = _dreamObjectEnumerator.MoveNext();
            if (_filterType != null) {
                while (hasNext && !_dreamObjectEnumerator.Current.IsSubtypeOf(_filterType)) {
                    hasNext = _dreamObjectEnumerator.MoveNext();
                }
            }

            return hasNext;
        }
    }

    /// <summary>
    /// Enumerates over an array of DreamValues
    /// <code>for (var/i in list(1, 2, 3))</code>
    /// </summary>
    sealed class DreamValueArrayEnumerator : IDreamValueEnumerator {
        private readonly DreamValue[]? _dreamValueArray;
        private int _current = -1;

        public DreamValueArrayEnumerator(DreamValue[]? dreamValueArray) {
            _dreamValueArray = dreamValueArray;
        }

        public DreamValue Current {
            get {
                if (_dreamValueArray == null)
                    return DreamValue.Null;
                if (_current < _dreamValueArray.Length)
                    return _dreamValueArray[_current];
                return DreamValue.Null;
            }
        }

        public bool MoveNext() {
            if (_dreamValueArray == null)
                return false;

            _current++;
            if (_current >= _dreamValueArray.Length)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Enumerates over an array of DreamValues, filtering for a certain type
    /// <code>for (var/obj/item/I in contents)</code>
    /// </summary>
    sealed class FilteredDreamValueArrayEnumerator : IDreamValueEnumerator {
        private readonly DreamValue[]? _dreamValueArray;
        private readonly IDreamObjectTree.TreeEntry _filterType;
        private int _current = -1;

        public FilteredDreamValueArrayEnumerator(DreamValue[]? dreamValueArray, IDreamObjectTree.TreeEntry filterType) {
            _dreamValueArray = dreamValueArray;
            _filterType = filterType;
        }

        public DreamValue Current {
            get {
                if (_dreamValueArray == null)
                    return DreamValue.Null;
                if (_current < _dreamValueArray.Length)
                    return _dreamValueArray[_current];
                return DreamValue.Null;
            }
        }

        public bool MoveNext() {
            if (_dreamValueArray == null)
                return false;

            do {
                _current++;
                if (_current >= _dreamValueArray.Length)
                    return false;

                DreamValue value = _dreamValueArray[_current];
                if (value.TryGetValueAsDreamObjectOfType(_filterType, out _))
                    return true;
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

        public DreamValue Current {
            get {
                if (_current < _mapManager.AllAtoms.Count)
                    return new(_mapManager.AllAtoms[_current]);
                return DreamValue.Null;
            }
        }

        public bool MoveNext() {
            do {
                _current++;
                if (_current >= _mapManager.AllAtoms.Count)
                    return false;

                if (_filterType != null) {
                    if (_mapManager.AllAtoms[_current].IsSubtypeOf(_filterType))
                        return true;
                } else {
                    return true;
                }
            } while (true);
        }
    }
}
